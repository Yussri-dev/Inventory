using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.Ui.Services.Sync
{
    public sealed class SaleLocalSyncPayloadBuilder
    : ILocalSyncPayloadBuilder
    {
        private const string SaleEntityName =
            "Sale";

        private const string CreateOperation =
            "Create";

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public SaleLocalSyncPayloadBuilder(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext)
        {
            ArgumentNullException.ThrowIfNull(
                db);

            ArgumentNullException.ThrowIfNull(
                tenantContext);

            _db =
                db;

            _tenantContext =
                tenantContext;
        }

        public string EntityName =>
            SaleEntityName;

        public async Task<string> BuildPayloadJsonAsync(
            SyncQueueItem queueItem,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                queueItem);

            cancellationToken
                .ThrowIfCancellationRequested();

            ValidateQueueItem(
                queueItem);

            var tenantId =
                _tenantContext
                    .GetRequiredTenantId();

            if (queueItem.TenantId != tenantId)
            {
                throw new InvalidOperationException(
                    "The Sale queue item belongs to another tenant.");
            }

            var sale =
                await _db.Sales
                    .AsNoTracking()
                    .Include(item =>
                        item.Lines)
                    .Include(item =>
                        item.Payments)
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.Id ==
                                queueItem.LocalEntityId,
                        cancellationToken);

            if (sale == null)
            {
                throw new InvalidOperationException(
                    $"Local Sale '{queueItem.LocalEntityId}' " +
                    "was not found.");
            }

            if (!string.Equals(
                    sale.Status,
                    LocalSaleStatus.Completed,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Local Sale '{sale.Id}' is not completed. " +
                    $"Current status: '{sale.Status}'.");
            }

            if (sale.ClientOperationId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Local Sale '{sale.Id}' has no " +
                    "ClientOperationId.");
            }

            if (sale.ClientOperationId !=
                queueItem.ClientOperationId)
            {
                throw new InvalidOperationException(
                    $"Local Sale '{sale.Id}' and queue item " +
                    $"'{queueItem.Id}' have different " +
                    "ClientOperationId values.");
            }

            /*
             * The existing mapper contains the authoritative mapping:
             *
             * - CashSessionServerId -> CashSessionId
             * - CustomerServerId -> CustomerId
             * - ProductServerId -> ProductId
             * - Method -> PaymentMethod
             * - TransactionRef -> Reference
             * - local line discounts -> effective DiscountPercent
             */
            var request =
                LocalSaleSyncMapper
                    .ToCreateCompleteSaleRequest(
                        sale);

            /*
             * The queue envelope remains authoritative for
             * synchronization idempotence.
             */
            request.ClientOperationId =
                queueItem.ClientOperationId;

            /*
             * A SQLite LocalSale.Id must never be interpreted as a
             * PostgreSQL pending Sale identifier.
             */
            request.PendingSaleId =
                null;

            return JsonSerializer.Serialize(
                request);
        }

        private static void ValidateQueueItem(
            SyncQueueItem queueItem)
        {
            if (!string.Equals(
                    queueItem.EntityName,
                    SaleEntityName,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Queue item '{queueItem.Id}' is not a " +
                    "Sale operation.");
            }

            if (!string.Equals(
                    queueItem.Operation,
                    CreateOperation,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Unsupported Sale operation " +
                    $"'{queueItem.Operation}'.");
            }

            if (queueItem.LocalEntityId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Sale queue item '{queueItem.Id}' has no " +
                    "LocalEntityId.");
            }

            if (queueItem.ClientOperationId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    $"Sale queue item '{queueItem.Id}' has no " +
                    "ClientOperationId.");
            }
        }
    }
}
