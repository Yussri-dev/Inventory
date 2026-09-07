#if DEBUG
using Inventory.Dto.Products.Requests;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.Ui.Services.Sync.Testing
{
    public sealed class ProductBulkUpdateSeeder
    {
        private const int MaximumCount =
            5_000;

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public ProductBulkUpdateSeeder(
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

        public async Task<int> SeedAsync(
            int count = 100,
            CancellationToken cancellationToken = default)
        {
            count =
                Math.Clamp(
                    count,
                    1,
                    MaximumCount);

            var tenantId =
                _tenantContext
                    .GetRequiredTenantId();

            var products =
                await _db.Products
                    .Where(product =>
                        product.TenantId == tenantId &&
                        !product.IsDeletedLocally &&
                        product.ServerId.HasValue &&
                        product.ServerId.Value != Guid.Empty &&
                        product.SalePrice >= 0m &&
                        product.SalePrice2 >= 0m &&
                        product.SalePrice3 >= 0m &&
                        product.PurchasePrice >= 0m &&
                        product.PurchasePrice <=
                            product.SalePrice &&
                        product.PurchasePrice <=
                            product.SalePrice2 &&
                        product.PurchasePrice <=
                            product.SalePrice3 &&
                        product.VatRate >= 0m &&
                        product.VatRate <= 100m &&
                        product.MinStockLevel >= 0m &&
                        product.MaxStockLevel >=
                            product.MinStockLevel)
                    .OrderBy(product =>
                        product.Id)
                    .Take(count)
                    .ToListAsync(
                        cancellationToken);

            if (products.Count < count)
            {
                throw new InvalidOperationException(
                    $"The Product update test requires {count} valid " +
                    "server-backed local Products, but only " +
                    $"{products.Count} are available.");
            }

            var now =
                DateTime.UtcNow;

            var queueItems =
                new List<SyncQueueItem>(
                    count);

            for (var index = 0;
                 index < products.Count;
                 index++)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var product =
                    products[index];

                var createdAtUtc =
                    now.AddTicks(
                        index + 1L);

                var request =
                    new UpdateProductRequest
                    {
                        SalePrice =
                            product.SalePrice,

                        SalePrice2 =
                            product.SalePrice2,

                        SalePrice3 =
                            product.SalePrice3,

                        PurchasePrice =
                            product.PurchasePrice,

                        VatRate =
                            product.VatRate,

                        MinStockLevel =
                            product.MinStockLevel,

                        MaxStockLevel =
                            product.MaxStockLevel,

                        IsTracked =
                            product.IsTracked,

                        IsActive =
                            product.Status
                    };

                product.SyncStatus =
                    SyncQueueStatus.Pending;

                queueItems.Add(
                    new SyncQueueItem
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        ClientOperationId =
                            Guid.NewGuid(),

                        LocalEntityId =
                            product.Id,

                        ServerEntityId =
                            product.ServerId,

                        EntityName =
                            "Product",

                        Operation =
                            SyncOperation.Update,

                        PayloadJson =
                            JsonSerializer.Serialize(
                                request),

                        Status =
                            SyncQueueStatus.Pending,

                        Attempts =
                            0,

                        ErrorMessage =
                            null,

                        CreatedAtUtc =
                            createdAtUtc,

                        LastAttemptAtUtc =
                            null,

                        ProcessedAtUtc =
                            null,

                        NextAttemptAtUtc =
                            null,

                        BatchId =
                            null,

                        LockedAtUtc =
                            null
                    });
            }

            await using var transaction =
                await _db.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                _db.SyncQueueItems.AddRange(
                    queueItems);

                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _db.ChangeTracker.Clear();

                return count;
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                _db.ChangeTracker.Clear();

                throw;
            }
        }
    }
}
#endif