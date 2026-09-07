using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services.Sync.Handlers
{
    public sealed class SaleLocalSyncResultHandler
        : ILocalSyncResultHandler
    {
        private const string SaleEntityName =
            "Sale";

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public SaleLocalSyncResultHandler(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext)
        {
            _db =
                db;

            _tenantContext =
                tenantContext;
        }

        public string EntityName =>
            SaleEntityName;

        public async Task ApplyAsync(
            SyncQueueItem queueItem,
            SyncBatchItemResult result,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                queueItem);

            ArgumentNullException.ThrowIfNull(
                result);

            if (!result.IsSuccessful)
            {
                return;
            }

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            if (queueItem.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    "The Sale synchronization result belongs " +
                    "to another tenant.");
            }

            var sale =
                await _db.Sales
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id ==
                                queueItem.LocalEntityId,
                        cancellationToken);

            if (sale == null)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Local Sale '{queueItem.LocalEntityId}' " +
                    "was not found.";

                return;
            }

            if (sale.ClientOperationId == Guid.Empty ||
                sale.ClientOperationId !=
                    queueItem.ClientOperationId)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Sale '{sale.Id}' has an invalid " +
                    "ClientOperationId.";

                return;
            }

            var serverSaleId =
                result.ServerEntityId ??
                queueItem.ServerEntityId ??
                sale.ServerId;

            if (!serverSaleId.HasValue ||
                serverSaleId.Value == Guid.Empty)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    "The server returned no Sale identifier.";

                return;
            }

            if (sale.ServerId.HasValue &&
                sale.ServerId.Value != Guid.Empty &&
                sale.ServerId.Value !=
                    serverSaleId.Value)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Local Sale '{sale.Id}' is already linked " +
                    "to another server Sale.";

                return;
            }

            var trackedOwner =
                _db.ChangeTracker
                    .Entries<LocalSale>()
                    .Select(entry =>
                        entry.Entity)
                    .FirstOrDefault(item =>
                        item.TenantId == tenantId &&
                        item.Id != sale.Id &&
                        item.ServerId ==
                            serverSaleId.Value);

            var existingOwner =
                trackedOwner ??
                await _db.Sales
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id != sale.Id &&
                            item.ServerId ==
                                serverSaleId.Value,
                        cancellationToken);

            if (existingOwner != null)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Server Sale '{serverSaleId.Value}' is " +
                    $"already linked to local Sale " +
                    $"'{existingOwner.Id}'.";

                return;
            }

            var synchronizedAtUtc =
                DateTime.UtcNow;

            sale.ServerId =
                serverSaleId.Value;

            if (!string.IsNullOrWhiteSpace(
                    result.ServerReferenceNumber))
            {
                sale.ServerInvoiceNumber =
                    result.ServerReferenceNumber.Trim();
            }

            sale.SyncStatus =
                SyncQueueStatus.Done;

            sale.LastSyncedAtUtc =
                synchronizedAtUtc;

            /*
             * Payments are created by CreateCompleteAsync.
             * They must not be uploaded separately.
             */
            var payments =
                await _db.Payments
                    .Where(payment =>
                        payment.TenantId == tenantId &&
                        payment.LocalSaleId ==
                            sale.Id)
                    .ToListAsync(
                        cancellationToken);

            foreach (var payment in payments)
            {
                payment.ServerSaleId =
                    serverSaleId.Value;

                payment.SyncStatus =
                    SyncQueueStatus.Done;

                payment.LastSyncedAtUtc =
                    synchronizedAtUtc;
            }

            /*
             * Server stock movements are created atomically
             * with the Sale.
             */
            var stockMovements =
                await _db.StockMovements
                    .Where(movement =>
                        movement.TenantId == tenantId &&
                        movement.LocalReferenceId ==
                            sale.Id)
                    .ToListAsync(
                        cancellationToken);

            foreach (var movement in stockMovements)
            {
                movement.ServerReferenceId =
                    serverSaleId.Value;

                movement.SyncStatus =
                    SyncQueueStatus.Done;

                movement.LastSyncedAtUtc =
                    synchronizedAtUtc;
            }

            /*
             * Server cash movements are also created
             * by CreateCompleteAsync.
             */
            var cashMovements =
                await _db.CashMovements
                    .Where(movement =>
                        movement.TenantId == tenantId &&
                        movement.LocalReferenceId ==
                            sale.Id)
                    .ToListAsync(
                        cancellationToken);

            foreach (var movement in cashMovements)
            {
                movement.ServerReferenceId =
                    serverSaleId.Value;

                movement.SyncStatus =
                    SyncQueueStatus.Done;

                movement.LastSyncedAtUtc =
                    synchronizedAtUtc;
            }

            /*
             * Credit transactions are generated on the server
             * by the Sale service and must not be uploaded
             * independently.
             */
            var customerTransactions =
                await _db.CustomerTransactions
                    .Where(transaction =>
                        transaction.TenantId == tenantId &&
                        transaction.SaleLocalId ==
                            sale.Id &&
                        transaction.Origin ==
                            LocalCustomerTransactionOrigin.Sale &&
                        !transaction.UploadRequired)
                    .ToListAsync(
                        cancellationToken);

            foreach (var transaction in customerTransactions)
            {
                transaction.SaleServerId =
                    serverSaleId.Value;

                transaction.SyncStatus =
                    SyncQueueStatus.Done;

                transaction.LastSyncedAtUtc =
                    synchronizedAtUtc;
            }

            /*
             * Do not call SaveChangesAsync here.
             * SyncQueueService persists the Sale reconciliation and
             * queue status in one SQLite transaction.
             *
             * Older duplicate results can have no
             * ServerReferenceNumber because they were processed
             * before that field existed.
             */
        }
    }
}