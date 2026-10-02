using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services.Sync.Handlers
{
    public sealed class CustomerLocalSyncResultHandler
        : ILocalSyncResultHandler
    {
        private const string CustomerEntityName =
            "Customer";

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public CustomerLocalSyncResultHandler(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext)
        {
            _db =
                db;

            _tenantContext =
                tenantContext;
        }

        public string EntityName =>
            CustomerEntityName;

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
                    "The Customer synchronization result belongs " +
                    "to another tenant.");
            }

            var customer =
                await _db.Customers
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id ==
                                queueItem.LocalEntityId,
                        cancellationToken);

            if (customer == null)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Local Customer '{queueItem.LocalEntityId}' " +
                    "was not found.";

                return;
            }

            var serverEntityId =
                result.ServerEntityId ??
                queueItem.ServerEntityId ??
                customer.ServerId;

            if (!serverEntityId.HasValue ||
                serverEntityId.Value == Guid.Empty)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    "The server returned no Customer identifier.";

                return;
            }

            if (customer.ServerId.HasValue &&
                customer.ServerId.Value != Guid.Empty &&
                customer.ServerId.Value !=
                    serverEntityId.Value)
            {
                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Local Customer '{customer.Id}' is already " +
                    "linked to a different server identifier.";

                return;
            }

            /*
             * SaveChanges is performed only after every result has been
             * applied. A previous result from the same batch can therefore
             * own the ServerId only in the EF Core change tracker.
             */
            var trackedOwner =
                _db.ChangeTracker
                    .Entries<LocalCustomer>()
                    .Select(entry =>
                        entry.Entity)
                    .FirstOrDefault(item =>
                        item.TenantId == tenantId &&
                        item.Id != customer.Id &&
                        item.ServerId ==
                            serverEntityId.Value);

            var existingOwner =
                trackedOwner ??
                await _db.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id != customer.Id &&
                            item.ServerId ==
                                serverEntityId.Value,
                        cancellationToken);

            if (existingOwner != null)
            {
                if (await CustomerDuplicateReconciler.TryArchiveAsync(_db, queueItem, customer, existingOwner, cancellationToken))
                    return;

                result.Status =
                    SyncBatchItemStatus.Conflict;

                result.ErrorMessage =
                    $"Server Customer '{serverEntityId.Value}' " +
                    $"is already linked to local Customer " +
                    $"'{existingOwner.Id}'.";

                return;
            }

            var now =
                DateTime.UtcNow;

            customer.ServerId =
                serverEntityId.Value;

            customer.SyncStatus =
                SyncQueueStatus.Done;

            customer.LastSyncedAtUtc =
                now;

            /*
             * Propagate the server customer id to local sales that
             * were created before the customer was synchronized.
             */
            var sales =
                await _db.Sales
                    .Where(sale =>
                        sale.TenantId == tenantId &&
                        sale.CustomerLocalId ==
                            customer.Id &&
                        (
                            sale.CustomerServerId == null ||
                            sale.CustomerServerId ==
                                Guid.Empty
                        ))
                    .ToListAsync(
                        cancellationToken);

            foreach (var sale in sales)
            {
                sale.CustomerServerId =
                    serverEntityId.Value;
            }

            /*
             * Customer payments/refunds created offline must use the
             * server customer id during their later synchronization.
             */
            var customerTransactions =
                await _db.CustomerTransactions
                    .Where(transaction =>
                        transaction.TenantId == tenantId &&
                        transaction.CustomerLocalId ==
                            customer.Id &&
                        (
                            transaction.CustomerServerId == null ||
                            transaction.CustomerServerId ==
                                Guid.Empty
                        ))
                    .ToListAsync(
                        cancellationToken);

            foreach (var customerTransaction in
                     customerTransactions)
            {
                customerTransaction.CustomerServerId =
                    serverEntityId.Value;
            }

            /*
             * Returns can also be created locally before Customer
             * synchronization completes.
             */
            var returns =
                await _db.Returns
                    .Where(localReturn =>
                        localReturn.TenantId == tenantId &&
                        localReturn.CustomerLocalId ==
                            customer.Id &&
                        (
                            localReturn.CustomerServerId == null ||
                            localReturn.CustomerServerId ==
                                Guid.Empty
                        ))
                    .ToListAsync(
                        cancellationToken);

            foreach (var localReturn in returns)
            {
                localReturn.CustomerServerId =
                    serverEntityId.Value;
            }

            /*
             * SaveChangesAsync is intentionally absent.
             * SyncQueueService saves the entity changes and queue statuses
             * together in one SQLite transaction.
             */
        }
    }
}
