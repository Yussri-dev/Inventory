using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services.Sync.Handlers
{
    public sealed class ProductLocalSyncResultHandler
        : ILocalSyncResultHandler
    {
        private const string ProductEntityName =
            "Product";

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public ProductLocalSyncResultHandler(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext)
        {
            _db =
                db;

            _tenantContext =
                tenantContext;
        }

        public string EntityName =>
            ProductEntityName;

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
                    "The Product synchronization result belongs " +
                    "to another tenant.");
            }

            var product =
                await _db.Products
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id ==
                                queueItem.LocalEntityId,
                        cancellationToken);

            if (product == null)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Local Product '{queueItem.LocalEntityId}' " +
                    "was not found.";

                return;
            }

            var serverEntityId =
                result.ServerEntityId ??
                queueItem.ServerEntityId ??
                product.ServerId;

            if (!serverEntityId.HasValue ||
                serverEntityId.Value == Guid.Empty)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    "The server returned no Product identifier.";

                return;
            }

            if (product.ServerId.HasValue &&
                product.ServerId.Value != Guid.Empty &&
                product.ServerId.Value !=
                    serverEntityId.Value)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Local Product '{product.Id}' is already " +
                    "linked to a different server identifier.";

                return;
            }

            /*
             * Results are applied before the single SQLite SaveChanges.
             * Check both tracked entities and persisted rows.
             */
            var trackedOwner =
                _db.ChangeTracker
                    .Entries<LocalProduct>()
                    .Select(entry =>
                        entry.Entity)
                    .FirstOrDefault(item =>
                        item.TenantId == tenantId &&
                        item.Id != product.Id &&
                        item.ServerId ==
                            serverEntityId.Value);

            var existingOwner =
                trackedOwner ??
                await _db.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id != product.Id &&
                            item.ServerId ==
                                serverEntityId.Value,
                        cancellationToken);

            if (existingOwner != null)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Server Product '{serverEntityId.Value}' " +
                    $"is already linked to local Product " +
                    $"'{existingOwner.Id}'.";

                return;
            }

            var now =
                DateTime.UtcNow;

            product.ServerId =
                serverEntityId.Value;

            product.SyncStatus =
                SyncQueueStatus.Done;

            product.LastSyncedAtUtc =
                now;

            /*
             * A pack can reference a unit Product created offline.
             * Fill only its server reference; stock is not modified here.
             */
            var dependentPacks =
                await _db.Products
                    .Where(item =>
                        item.TenantId == tenantId &&
                        item.UnitProductLocalId == product.Id &&
                        (
                            item.UnitProductServerId == null ||
                            item.UnitProductServerId ==
                                Guid.Empty
                        ))
                    .ToListAsync(
                        cancellationToken);

            foreach (var dependentPack in dependentPacks)
            {
                dependentPack.UnitProductServerId =
                    serverEntityId.Value;
            }

            /*
             * SaveChangesAsync is intentionally absent.
             * SyncQueueService saves the Product changes and queue statuses
             * together in one SQLite transaction.
             */
        }
    }
}