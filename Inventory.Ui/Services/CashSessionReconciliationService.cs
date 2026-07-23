using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.Services
{
    public sealed class CashSessionReconciliationService
     : ICashSessionReconciliationService
    {
        private readonly PosLocalDbContext _db;
        private readonly ICashSessionApi _cashSessionApi;
        private readonly ILocalTenantContext _tenantContext;

        public CashSessionReconciliationService(
            PosLocalDbContext db,
            ICashSessionApi cashSessionApi,
            ILocalTenantContext tenantContext)
        {
            _db = db;
            _cashSessionApi = cashSessionApi;
            _tenantContext = tenantContext;
        }

        public async Task<CashSessionReconciliationResult> InspectAsync(
            CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            var localSession =
                await _db.CashSessions
                    .FirstOrDefaultAsync(
                        session =>
                            session.TenantId == tenantId &&
                            session.Status ==
                                LocalCashSessionStatus.Open,
                        cancellationToken);

            /*
             * This call intentionally happens here rather than inside the
             * local cash-session service. LocalCashSessionService remains
             * fully offline-capable.
             */
            var serverSession =
                await _cashSessionApi.GetActive(
                    cancellationToken);

            if (serverSession == null)
            {
                return new CashSessionReconciliationResult
                {
                    State =
                        CashSessionReconciliationState.Ready,

                    LocalSession =
                        localSession,

                    Message =
                        "No active server cash session was found."
                };
            }

            if (localSession == null)
            {
                return new CashSessionReconciliationResult
                {
                    State =
                        CashSessionReconciliationState.ServerSessionOnly,

                    ServerSession =
                        serverSession,

                    Message =
                        $"Server cash session " +
                        $"{serverSession.SessionNumber} is open, but this " +
                        "device has no matching open local session. Close " +
                        "the server session explicitly before opening a new " +
                        "local session."
                };
            }

            if (RepresentsSameSession(
                    localSession,
                    serverSession))
            {
                await LinkLocalSessionToServerAsync(
                    localSession,
                    serverSession,
                    cancellationToken);

                return new CashSessionReconciliationResult
                {
                    State =
                        CashSessionReconciliationState.MatchingSessionLinked,

                    LocalSession =
                        localSession,

                    ServerSession =
                        serverSession,

                    Message =
                        $"Local cash session " +
                        $"{localSession.SessionNumber} was linked to the " +
                        "matching server session."
                };
            }

            localSession.SyncStatus =
                SyncQueueStatus.Conflict;

            await _db.SaveChangesAsync(
                cancellationToken);

            return new CashSessionReconciliationResult
            {
                State =
                    CashSessionReconciliationState.Conflict,

                LocalSession =
                    localSession,

                ServerSession =
                    serverSession,

                Message =
                    $"Server session {serverSession.SessionNumber} and " +
                    $"local session {localSession.SessionNumber} represent " +
                    "different cash operations. They must not be merged. " +
                    "Count and close the stale server session, then retry " +
                    "synchronization."
            };
        }

        public async Task<CashSessionResult> CloseServerSessionAsync(
            Guid serverCashSessionId,
            decimal actualCash,
            string? closingNotes = null,
            CancellationToken cancellationToken = default)
        {
            if (serverCashSessionId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Server cash-session id is required.",
                    nameof(serverCashSessionId));
            }

            if (actualCash < 0m)
            {
                throw new InvalidOperationException(
                    "Actual cash cannot be negative.");
            }

            var closed =
                await _cashSessionApi.Close(
                    serverCashSessionId,
                    new CloseCashSessionRequest
                    {
                        ActualCash =
                            RoundMoney(actualCash),

                        ClosingNotes =
                            NormalizeNullable(closingNotes) ??
                            "Closed from cash-session conflict resolution."
                    },
                    cancellationToken);

            /*
             * Usually a stale server session has no local mirror. When a
             * linked mirror exists, keep SQLite aligned with the response.
             */
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            var localMirror =
                await _db.CashSessions
                    .FirstOrDefaultAsync(
                        session =>
                            session.TenantId == tenantId &&
                            session.ServerId ==
                                closed.Id,
                        cancellationToken);

            if (localMirror != null)
            {
                localMirror.Status =
                    LocalCashSessionStatus.Closed;

                localMirror.ClosedAtUtc =
                    closed.ClosedAt;

                localMirror.ClosingAmountExpected =
                    closed.ClosingAmountExpected;

                localMirror.ClosingAmountCounted =
                    closed.ClosingAmountCounted;

                localMirror.Difference =
                    closed.Difference;

                localMirror.ClosingNotes =
                    NormalizeNullable(
                        closed.ClosingNotes);

                localMirror.SyncStatus =
                    SyncQueueStatus.Done;

                localMirror.LastSyncedAtUtc =
                    DateTime.UtcNow;

                await _db.SaveChangesAsync(
                    cancellationToken);
            }

            return closed;
        }

        private static bool RepresentsSameSession(
            LocalCashSession localSession,
            CashSessionResult serverSession)
        {
            if (localSession.ServerId.HasValue &&
                localSession.ServerId.Value ==
                    serverSession.Id)
            {
                return true;
            }

            return localSession.ClientOperationId !=
                       Guid.Empty &&
                   serverSession.ClientOperationId !=
                       Guid.Empty &&
                   localSession.ClientOperationId ==
                       serverSession.ClientOperationId;
        }

        private async Task LinkLocalSessionToServerAsync(
            LocalCashSession localSession,
            CashSessionResult serverSession,
            CancellationToken cancellationToken)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            var now =
                DateTime.UtcNow;

            localSession.ServerId =
                serverSession.Id;

            localSession.SessionNumber =
                string.IsNullOrWhiteSpace(
                    serverSession.SessionNumber)
                    ? localSession.SessionNumber
                    : serverSession.SessionNumber;

            localSession.LastSyncedAtUtc =
                now;

            localSession.SyncStatus =
                localSession.Status ==
                    LocalCashSessionStatus.Closed
                    ? SyncQueueStatus.Pending
                    : SyncQueueStatus.Done;

            var sales =
                await _db.Sales
                    .Where(sale =>
                        sale.TenantId == tenantId &&
                        sale.LocalCashSessionId ==
                            localSession.Id)
                    .ToListAsync(
                        cancellationToken);

            foreach (var sale in sales)
            {
                sale.CashSessionServerId =
                    serverSession.Id;
            }

            var movements =
                await _db.CashMovements
                    .Where(movement =>
                        movement.TenantId == tenantId &&
                        movement.LocalCashSessionId ==
                            localSession.Id)
                    .ToListAsync(
                        cancellationToken);

            foreach (var movement in movements)
            {
                movement.ServerCashSessionId =
                    serverSession.Id;

                if (movement.Type ==
                    LocalCashMovementType.Opening)
                {
                    movement.SyncStatus =
                        SyncQueueStatus.Done;

                    movement.LastSyncedAtUtc =
                        now;
                }
            }

            await _db.SaveChangesAsync(
                cancellationToken);
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
