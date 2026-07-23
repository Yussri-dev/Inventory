using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services
{
    public sealed class LocalCashSessionService : ILocalCashSessionService
    {
        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public LocalCashSessionService(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<bool> HasOpenSessionAsync(
            CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            return await _db.CashSessions
                .AsNoTracking()
                .AnyAsync(
                    session =>
                        session.TenantId == tenantId &&
                        session.Status ==
                            LocalCashSessionStatus.Open,
                    cancellationToken);
        }

        public async Task<LocalCashSession?> GetOpenSessionAsync(
            CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            /*
             * Read at most two rows so corrupted legacy data is detected
             * instead of silently selecting one arbitrary open session.
             */
            var openSessions =
                await _db.CashSessions
                    .AsNoTracking()
                    .Where(session =>
                        session.TenantId == tenantId &&
                        session.Status ==
                            LocalCashSessionStatus.Open)
                    .OrderByDescending(session =>
                        session.OpenedAtUtc)
                    .Take(2)
                    .ToListAsync(
                        cancellationToken);

            if (openSessions.Count > 1)
            {
                throw new InvalidOperationException(
                    "Multiple open local cash sessions were found for " +
                    "the current tenant. Resolve the local data conflict " +
                    "before continuing.");
            }

            return openSessions.FirstOrDefault();
        }

        public async Task<LocalCashSession?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "Cash session id is required.",
                    nameof(id));
            }

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            return await _db.CashSessions
                .AsNoTracking()
                .Include(session =>
                    session.CashMovements)
                .Include(session =>
                    session.Sales)
                .FirstOrDefaultAsync(
                    session =>
                        session.TenantId == tenantId &&
                        session.Id == id,
                    cancellationToken);
        }

        public async Task<List<LocalCashSession>> GetRecentAsync(
            int take = 20,
            CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            take =
                Math.Clamp(
                    take,
                    1,
                    100);

            return await _db.CashSessions
                .AsNoTracking()
                .Where(session =>
                    session.TenantId == tenantId)
                .OrderByDescending(session =>
                    session.OpenedAtUtc)
                .Take(take)
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<LocalCashSession> OpenAsync(
            decimal openingAmount,
            Guid? openedByUserId = null,
            string? openingNotes = null,
            CancellationToken cancellationToken = default)
        {
            if (openingAmount < 0m)
            {
                throw new InvalidOperationException(
                    "Opening amount cannot be negative.");
            }

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            await using var databaseTransaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var existingOpenSession =
                    await _db.CashSessions
                        .FirstOrDefaultAsync(
                            session =>
                                session.TenantId == tenantId &&
                                session.Status ==
                                    LocalCashSessionStatus.Open,
                            cancellationToken);

                if (existingOpenSession != null)
                {
                    throw new InvalidOperationException(
                        $"Cash session " +
                        $"'{existingOpenSession.SessionNumber}' " +
                        "is already open for the current tenant.");
                }

                var now =
                    DateTime.UtcNow;

                var session =
                    new LocalCashSession
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        ServerId =
                            null,

                        ClientOperationId =
                            Guid.NewGuid(),

                        SessionNumber =
                            GenerateSessionNumber(now),

                        OpenedAtUtc =
                            now,

                        OpeningAmount =
                            RoundMoney(openingAmount),

                        ClosingAmountExpected =
                            RoundMoney(openingAmount),

                        ClosingAmountCounted =
                            0m,

                        Difference =
                            0m,

                        Status =
                            LocalCashSessionStatus.Open,

                        OpenedByUserId =
                            openedByUserId,

                        OpeningNotes =
                            NormalizeNullable(openingNotes),

                        SyncStatus =
                            SyncQueueStatus.Pending,

                        CreatedAtUtc =
                            now
                    };

                var openingMovement =
                    new LocalCashMovement
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        ServerId =
                            null,

                        ClientOperationId =
                            Guid.NewGuid(),

                        LocalCashSessionId =
                            session.Id,

                        ServerCashSessionId =
                            null,

                        Type =
                            LocalCashMovementType.Opening,

                        Amount =
                            RoundMoney(openingAmount),

                        ReferenceNumber =
                            session.SessionNumber,

                        Notes =
                            "Opening cash amount",

                        MovementDateUtc =
                            now,

                        SyncStatus =
                            SyncQueueStatus.Pending
                    };

                _db.CashSessions.Add(
                    session);

                _db.CashMovements.Add(
                    openingMovement);

                await _db.SaveChangesAsync(
                    cancellationToken);

                await databaseTransaction.CommitAsync(
                    cancellationToken);

                return session;
            }
            catch
            {
                await databaseTransaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }

        public async Task<LocalCashSession> CloseAsync(
            Guid localCashSessionId,
            decimal countedAmount,
            Guid? closedByUserId = null,
            string? closingNotes = null,
            CancellationToken cancellationToken = default)
        {
            if (localCashSessionId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Cash session id is required.",
                    nameof(localCashSessionId));
            }

            if (countedAmount < 0m)
            {
                throw new InvalidOperationException(
                    "Counted amount cannot be negative.");
            }

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            await using var databaseTransaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var session =
                    await _db.CashSessions
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId &&
                                item.Id == localCashSessionId,
                            cancellationToken)
                    ?? throw new InvalidOperationException(
                        "Cash session was not found for the current tenant.");

                var roundedCountedAmount =
                    RoundMoney(countedAmount);

                /*
                 * A retry after the local transaction was committed must
                 * not create a second closing report.
                 */
                if (session.Status ==
                    LocalCashSessionStatus.Closed)
                {
                    if (session.ClosingAmountCounted !=
                        roundedCountedAmount)
                    {
                        throw new InvalidOperationException(
                            "The cash session is already closed with a " +
                            "different counted amount.");
                    }

                    await databaseTransaction.CommitAsync(
                        cancellationToken);

                    return session;
                }

                if (session.Status !=
                    LocalCashSessionStatus.Open)
                {
                    throw new InvalidOperationException(
                        "Only an open cash session can be closed.");
                }

                /*
                 * The SQLite provider does not reliably support decimal
                 * SUM expressions. Load the signed amounts first, then
                 * calculate the sum with LINQ to Objects.
                 */
                var movementAmounts =
                    await _db.CashMovements
                        .AsNoTracking()
                        .Where(movement =>
                            movement.TenantId == tenantId &&
                            movement.LocalCashSessionId ==
                                localCashSessionId)
                        .Select(movement =>
                            movement.Amount)
                        .ToListAsync(
                            cancellationToken);

                var expectedAmount =
                    RoundMoney(
                        movementAmounts.Sum());

                var cashSaleAmounts =
                    await _db.CashMovements
                        .AsNoTracking()
                        .Where(movement =>
                            movement.TenantId == tenantId &&
                            movement.LocalCashSessionId ==
                                localCashSessionId &&
                            movement.Type ==
                                LocalCashMovementType.Sale)
                        .Select(movement =>
                            movement.Amount)
                        .ToListAsync(
                            cancellationToken);

                var cashSales =
                    RoundMoney(
                        cashSaleAmounts.Sum());

                var totalTransactions =
                    await _db.Sales
                        .AsNoTracking()
                        .CountAsync(
                            sale =>
                                sale.TenantId == tenantId &&
                                sale.LocalCashSessionId ==
                                    localCashSessionId,
                            cancellationToken);

                var now =
                    DateTime.UtcNow;

                var difference =
                    RoundMoney(
                        roundedCountedAmount -
                        expectedAmount);

                session.ClosedAtUtc =
                    now;

                session.ClosedByUserId =
                    closedByUserId;

                session.ClosingAmountExpected =
                    expectedAmount;

                session.ClosingAmountCounted =
                    roundedCountedAmount;

                session.Difference =
                    difference;

                session.ClosingNotes =
                    NormalizeNullable(closingNotes);

                session.Status =
                    LocalCashSessionStatus.Closed;

                session.SyncStatus =
                    SyncQueueStatus.Pending;

                var existingClosingReport =
                    await _db.CashReports
                        .FirstOrDefaultAsync(
                            report =>
                                report.TenantId == tenantId &&
                                report.LocalCashSessionId ==
                                    session.Id &&
                                report.Type ==
                                    LocalCashReportType.Closing,
                            cancellationToken);

                if (existingClosingReport == null)
                {
                    var report =
                        new LocalCashReport
                        {
                            Id =
                                Guid.NewGuid(),

                            TenantId =
                                tenantId,

                            LocalCashSessionId =
                                session.Id,

                            Type =
                                LocalCashReportType.Closing,

                            ExpectedAmount =
                                expectedAmount,

                            CountedAmount =
                                roundedCountedAmount,

                            Difference =
                                difference,

                            CashSales =
                                cashSales,

                            CardSales =
                                0m,

                            OtherPayments =
                                0m,

                            TotalTransactions =
                                totalTransactions,

                            GeneratedAtUtc =
                                now,

                            GeneratedByUserId =
                                closedByUserId,

                            Notes =
                                NormalizeNullable(closingNotes),

                            SyncStatus =
                                SyncQueueStatus.Pending
                        };

                    _db.CashReports.Add(
                        report);
                }

                await _db.SaveChangesAsync(
                    cancellationToken);

                await databaseTransaction.CommitAsync(
                    cancellationToken);

                return session;
            }
            catch
            {
                await databaseTransaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }

        private static string GenerateSessionNumber(
            DateTime utcNow)
        {
            return $"CS-{utcNow:yyyyMMdd-HHmmss}";
        }

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static string? NormalizeNullable(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}