using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace Inventory.LocalDB.Services
{
    public class LocalCashSessionService : ILocalCashSessionService
    {
        private readonly PosLocalDbContext _db;

        public LocalCashSessionService(PosLocalDbContext db)
        {
            _db = db;
        }

        public async Task<bool> HasOpenSessionAsync(CancellationToken cancellationToken = default)
        {
            return await _db.CashSessions
                .AsNoTracking()
                .AnyAsync(x => x.Status == LocalCashSessionStatus.Open, cancellationToken);
        }

        public async Task<LocalCashSession?> GetOpenSessionAsync(CancellationToken cancellationToken = default)
        {
            return await _db.CashSessions
                .AsNoTracking()
                .OrderByDescending(x => x.OpenedAtUtc)
                .FirstOrDefaultAsync(x => x.Status == LocalCashSessionStatus.Open, cancellationToken);
        }

        public async Task<LocalCashSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _db.CashSessions
                .AsNoTracking()
                .Include(x => x.CashMovements)
                .Include(x => x.Sales)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<List<LocalCashSession>> GetRecentAsync(int take = 20, CancellationToken cancellationToken = default)
        {
            if (take <= 0)
                take = 20;

            return await _db.CashSessions
                .AsNoTracking()
                .OrderByDescending(x => x.OpenedAtUtc)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<LocalCashSession> OpenAsync(
            decimal openingAmount,
            Guid? openedByUserId = null,
            string? openingNotes = null,
            CancellationToken cancellationToken = default)
        {
            if (openingAmount < 0)
                throw new InvalidOperationException("Opening amount cannot be negative.");

            var hasOpenSession = await HasOpenSessionAsync(cancellationToken);

            if (hasOpenSession)
                throw new InvalidOperationException("There is already an open cash session.");

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var now = DateTime.UtcNow;

            var session = new LocalCashSession
            {
                Id = Guid.NewGuid(),
                ClientOperationId = Guid.NewGuid(),
                SessionNumber = GenerateSessionNumber(now),
                OpenedAtUtc = now,
                OpeningAmount = openingAmount,
                ClosingAmountExpected = openingAmount,
                ClosingAmountCounted = 0,
                Difference = 0,
                Status = LocalCashSessionStatus.Open,
                OpenedByUserId = openedByUserId,
                OpeningNotes = openingNotes,
                SyncStatus = SyncQueueStatus.Pending,
                CreatedAtUtc = now
            };

            var openingMovement = new LocalCashMovement
            {
                Id = Guid.NewGuid(),
                ClientOperationId = Guid.NewGuid(),
                LocalCashSessionId = session.Id,
                Type = LocalCashMovementType.Opening,
                Amount = openingAmount,
                ReferenceNumber = session.SessionNumber,
                Notes = "Opening cash amount",
                MovementDateUtc = now,
                SyncStatus = SyncQueueStatus.Pending
            };

            _db.CashSessions.Add(session);
            _db.CashMovements.Add(openingMovement);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return session;
        }

        public async Task<LocalCashSession> CloseAsync(
            Guid localCashSessionId,
            decimal countedAmount,
            Guid? closedByUserId = null,
            string? closingNotes = null,
            CancellationToken cancellationToken = default)
        {
            if (countedAmount < 0)
                throw new InvalidOperationException("Counted amount cannot be negative.");

            var session = await _db.CashSessions
                .FirstOrDefaultAsync(x => x.Id == localCashSessionId, cancellationToken);

            if (session == null)
                throw new InvalidOperationException("Cash session was not found.");

            if (session.Status != LocalCashSessionStatus.Open)
                throw new InvalidOperationException("Only an open cash session can be closed.");

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var expectedAmount = await _db.CashMovements
                .Where(x => x.LocalCashSessionId == localCashSessionId)
                .SumAsync(x => x.Amount, cancellationToken);

            var now = DateTime.UtcNow;

            session.ClosedAtUtc = now;
            session.ClosedByUserId = closedByUserId;
            session.ClosingAmountExpected = expectedAmount;
            session.ClosingAmountCounted = countedAmount;
            session.Difference = countedAmount - expectedAmount;
            session.ClosingNotes = closingNotes;
            session.Status = LocalCashSessionStatus.Closed;
            session.SyncStatus = SyncQueueStatus.Pending;

            var cashSales = await _db.CashMovements
                .Where(x =>
                    x.LocalCashSessionId == localCashSessionId &&
                    x.Type == LocalCashMovementType.Sale)
                .SumAsync(x => x.Amount, cancellationToken);

            var totalTransactions = await _db.Sales
                .CountAsync(x => x.LocalCashSessionId == localCashSessionId, cancellationToken);

            var report = new LocalCashReport
            {
                Id = Guid.NewGuid(),
                LocalCashSessionId = session.Id,
                Type = LocalCashReportType.Closing,
                ExpectedAmount = expectedAmount,
                CountedAmount = countedAmount,
                Difference = countedAmount - expectedAmount,
                CashSales = cashSales,
                CardSales = 0,
                OtherPayments = 0,
                TotalTransactions = totalTransactions,
                GeneratedAtUtc = now,
                GeneratedByUserId = closedByUserId,
                Notes = closingNotes,
                SyncStatus = SyncQueueStatus.Pending
            };

            _db.CashReports.Add(report);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return session;
        }

        private static string GenerateSessionNumber(DateTime utcNow)
        {
            return $"CS-{utcNow:yyyyMMdd-HHmmss}";
        }
    }
}
