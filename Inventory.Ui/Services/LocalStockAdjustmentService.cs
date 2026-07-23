using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventory.Ui.Services
{
    public sealed class LocalStockAdjustmentService
        : ILocalStockAdjustmentService
    {
        private const string StockMovementEntityName =
            "StockMovement";

        private readonly PosLocalDbContext _db;
        private readonly ILocalTenantContext _tenantContext;

        public LocalStockAdjustmentService(
            PosLocalDbContext db,
            ILocalTenantContext tenantContext)
        {
            _db = db;
            _tenantContext = tenantContext;
        }

        public async Task<IReadOnlyList<LocalStock>> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            page =
                Math.Max(
                    1,
                    page);

            pageSize =
                Math.Clamp(
                    pageSize,
                    1,
                    200);

            var query =
                BuildSearchQuery(
                    tenantId,
                    search);

            return await query
                .AsNoTracking()
                .OrderBy(stock =>
                    stock.ProductName)
                .ThenBy(stock =>
                    stock.ProductBarcode)
                .Skip(
                    (page - 1) *
                    pageSize)
                .Take(
                    pageSize)
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<int> CountAsync(
            string? search,
            CancellationToken cancellationToken = default)
        {
            var tenantId =
                _tenantContext.GetRequiredTenantId();

            return await BuildSearchQuery(
                    tenantId,
                    search)
                .CountAsync(
                    cancellationToken);
        }

        public async Task<LocalStock> AdjustAsync(
            Guid localStockId,
            decimal newQuantity,
            string? notes,
            CancellationToken cancellationToken = default)
        {
            if (localStockId ==
                Guid.Empty)
            {
                throw new ArgumentException(
                    "Local stock id is required.",
                    nameof(localStockId));
            }

            newQuantity =
                RoundQuantity(
                    newQuantity);

            if (newQuantity <
                0m)
            {
                throw new InvalidOperationException(
                    "Stock quantity cannot be negative.");
            }

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var stock =
                    await _db.Stocks
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId &&
                                item.Id == localStockId,
                            cancellationToken)
                    ?? throw new KeyNotFoundException(
                        "The local stock was not found.");

                if (newQuantity <
                    stock.ReservedQuantity)
                {
                    throw new InvalidOperationException(
                        $"The new quantity cannot be lower than the " +
                        $"reserved quantity ({stock.ReservedQuantity:0.###}).");
                }

                var product =
                    await _db.Products
                        .FirstOrDefaultAsync(
                            item =>
                                item.TenantId == tenantId &&
                                item.Id ==
                                    stock.ProductLocalId &&
                                !item.IsDeletedLocally,
                            cancellationToken)
                    ?? throw new KeyNotFoundException(
                        "The local product linked to this stock was not found.");

                var quantityBefore =
                    RoundQuantity(
                        stock.Quantity);

                var quantityAfter =
                    newQuantity;

                var quantityChange =
                    RoundQuantity(
                        quantityAfter -
                        quantityBefore);

                if (quantityChange ==
                    0m)
                {
                    return stock;
                }

                var now =
                    DateTime.UtcNow;

                stock.Quantity =
                    quantityAfter;

                stock.LastUpdatedUtc =
                    now;

                product.LocalStockQuantity =
                    quantityAfter;

                var movement =
                    new LocalStockMovement
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        ServerId =
                            null,

                        ClientOperationId =
                            Guid.NewGuid(),

                        ProductLocalId =
                            stock.ProductLocalId,

                        ProductServerId =
                            stock.ProductServerId ??
                            product.ServerId ??
                            Guid.Empty,

                        ProductName =
                            string.IsNullOrWhiteSpace(
                                stock.ProductName)
                                ? product.Name
                                : stock.ProductName,

                        ProductBarcode =
                            stock.ProductBarcode ??
                            product.Barcode,

                        QuantityChange =
                            quantityChange,

                        QuantityBefore =
                            quantityBefore,

                        QuantityAfter =
                            quantityAfter,

                        Type =
                            LocalStockMovementType.Adjustment,

                        UnitCost =
                            RoundMoney(
                                product.PurchasePrice),

                        LocalReferenceId =
                            stock.Id,

                        ServerReferenceId =
                            stock.ServerId,

                        ReferenceNumber =
                            $"ADJ-{now:yyyyMMdd-HHmmssfff}",

                        Notes =
                            NormalizeNotes(
                                notes),

                        MovementDateUtc =
                            now,

                        SyncStatus =
                            SyncQueueStatus.Pending,

                        LastSyncedAtUtc =
                            null
                    };

                _db.StockMovements.Add(
                    movement);

                var payloadJson =
                    JsonSerializer.Serialize(
                        new
                        {
                            movement.Id,
                            movement.ClientOperationId,
                            movement.ProductLocalId,
                            movement.ProductServerId,
                            movement.QuantityChange,
                            movement.QuantityBefore,
                            movement.QuantityAfter,
                            movement.Type,
                            movement.Notes,
                            movement.MovementDateUtc
                        });

                _db.SyncQueueItems.Add(
                    new SyncQueueItem
                    {
                        Id =
                            Guid.NewGuid(),

                        TenantId =
                            tenantId,

                        LocalEntityId =
                            movement.Id,

                        ClientOperationId =
                            movement.ClientOperationId,

                        EntityName =
                            StockMovementEntityName,

                        Operation =
                            SyncOperation.Create,

                        PayloadJson =
                            payloadJson,

                        Status =
                            SyncQueueStatus.Pending,

                        Attempts =
                            0,

                        ErrorMessage =
                            null,

                        ProcessedAtUtc =
                            null,

                        CreatedAtUtc =
                            now
                    });

                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                return stock;
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }

        public async Task<IReadOnlyList<LocalStockMovement>>
            GetHistoryAsync(
                Guid productLocalId,
                int maximumResults = 100,
                CancellationToken cancellationToken = default)
        {
            if (productLocalId ==
                Guid.Empty)
            {
                throw new ArgumentException(
                    "Local product id is required.",
                    nameof(productLocalId));
            }

            var tenantId =
                _tenantContext.GetRequiredTenantId();

            maximumResults =
                Math.Clamp(
                    maximumResults,
                    1,
                    500);

            return await _db.StockMovements
                .AsNoTracking()
                .Where(movement =>
                    movement.TenantId == tenantId &&
                    movement.ProductLocalId ==
                        productLocalId)
                .OrderByDescending(movement =>
                    movement.MovementDateUtc)
                .Take(
                    maximumResults)
                .ToListAsync(
                    cancellationToken);
        }

        private IQueryable<LocalStock> BuildSearchQuery(
            Guid tenantId,
            string? search)
        {
            var query =
                _db.Stocks
                    .Where(stock =>
                        stock.TenantId ==
                        tenantId);

            if (string.IsNullOrWhiteSpace(
                    search))
            {
                return query;
            }

            var term =
                search.Trim();

            return query.Where(stock =>
                stock.ProductName.Contains(term) ||
                (stock.ProductBarcode != null &&
                 stock.ProductBarcode.Contains(term)));
        }

        private static decimal RoundQuantity(
            decimal value)
        {
            return Math.Round(
                value,
                3,
                MidpointRounding.AwayFromZero);
        }

        private static decimal RoundMoney(
            decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static string NormalizeNotes(
            string? notes)
        {
            var value =
                string.IsNullOrWhiteSpace(
                    notes)
                    ? "Manual stock adjustment"
                    : notes.Trim();

            return value.Length <= 500
                ? value
                : value[..500];
        }
    }
}
