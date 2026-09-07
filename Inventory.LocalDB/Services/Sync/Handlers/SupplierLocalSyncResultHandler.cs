using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services.Sync.Handlers
{
    public sealed class SupplierLocalSyncResultHandler
        : ILocalSyncResultHandler
    {
        private const string SupplierEntityName =
            "Supplier";

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public SupplierLocalSyncResultHandler(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext)
        {
            _db =
                db;

            _tenantContext =
                tenantContext;
        }

        public string EntityName =>
            SupplierEntityName;

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
                _tenantContext
                    .GetRequiredTenantId();

            if (queueItem.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    "The Supplier synchronization result belongs " +
                    "to another tenant.");
            }

            var supplier =
                await _db.Suppliers
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id ==
                                queueItem.LocalEntityId,
                        cancellationToken);

            if (supplier == null)
            {
                throw new InvalidOperationException(
                    $"Local Supplier '{queueItem.LocalEntityId}' " +
                    "was not found.");
            }

            var serverEntityId =
                result.ServerEntityId ??
                queueItem.ServerEntityId ??
                supplier.ServerId;

            if (!serverEntityId.HasValue ||
                serverEntityId.Value == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "The server returned no Supplier identifier.");
            }

            if (supplier.ServerId.HasValue &&
                supplier.ServerId.Value != Guid.Empty &&
                supplier.ServerId.Value !=
                    serverEntityId.Value)
            {
                throw new InvalidOperationException(
                    $"Local Supplier '{supplier.Id}' is already " +
                    "linked to a different server identifier.");
            }

            var trackedOwner =
                _db.ChangeTracker
                    .Entries<LocalSupplier>()
                    .Select(entry =>
                        entry.Entity)
                    .FirstOrDefault(item =>
                        item.TenantId == tenantId &&
                        item.Id != supplier.Id &&
                        item.ServerId ==
                            serverEntityId.Value);

            var existingOwner =
                trackedOwner ??
                await _db.Suppliers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id != supplier.Id &&
                            item.ServerId ==
                                serverEntityId.Value,
                        cancellationToken);

            if (existingOwner != null)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Server Supplier '{serverEntityId.Value}' " +
                    $"is already linked to local Supplier " +
                    $"'{existingOwner.Id}'.";

                return;
            }

            var now =
                DateTime.UtcNow;

            supplier.ServerId =
                serverEntityId.Value;

            supplier.SyncStatus =
                SyncQueueStatus.Done;

            supplier.LastSyncedAtUtc =
                now;

            /*
             * Propagate the server supplier id to local purchases that
             * were created before the supplier was synchronized.
             */
            var purchases =
                await _db.Purchases
                    .Where(purchase =>
                        purchase.TenantId == tenantId &&
                        purchase.SupplierLocalId ==
                            supplier.Id &&
                        (
                            purchase.SupplierServerId == null ||
                            purchase.SupplierServerId ==
                                Guid.Empty
                        ))
                    .ToListAsync(
                        cancellationToken);

            foreach (var purchase in purchases)
            {
                purchase.SupplierServerId =
                    serverEntityId.Value;
            }

            /*
             * SaveChangesAsync is intentionally absent.
             * SyncQueueService saves the local entity changes and queue
             * statuses together in one SQLite transaction.
             */
        }
    }
}