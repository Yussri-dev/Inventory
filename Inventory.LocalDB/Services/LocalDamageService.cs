using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.LocalDB.Services.Results;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services;

public sealed class LocalDamageService
    : ILocalDamageService
{
    private const string DamageEntityName =
        "Damage";

    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public LocalDamageService(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<LocalDamageProductResult>>
    SearchProductsAsync(
        string search,
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var normalizedSearch =
            search?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return Array.Empty<LocalDamageProductResult>();
        }

        take = Math.Clamp(
            take,
            1,
            50);

        var normalizedLower =
            normalizedSearch.ToLowerInvariant();

        var products =
            await _db.Products
                .AsNoTracking()
                .Where(product =>
                    product.TenantId == tenantId &&
                    !product.IsDeletedLocally &&
                    product.IsActive &&
                    (
                        product.Name
                            .ToLower()
                            .Contains(normalizedLower) ||

                        product.Barcode != null &&
                        product.Barcode
                            .ToLower()
                            .Contains(normalizedLower) ||

                        product.Sku != null &&
                        product.Sku
                            .ToLower()
                            .Contains(normalizedLower)
                    ))
                .OrderBy(product =>
                    product.Name)
                .Take(take)
                .Select(product => new
                {
                    product.Id,
                    product.ServerId,
                    product.Name,
                    product.Barcode,
                    product.PurchasePrice
                })
                .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return Array.Empty<LocalDamageProductResult>();
        }

        var productIds =
            products
                .Select(product => product.Id)
                .ToList();

        /*
         * ProductLocalId est un Guid obligatoire.
         * Aucun HasValue ou Value n'est nécessaire.
         */
        var stocks =
            await _db.Stocks
                .AsNoTracking()
                .Where(stock =>
                    stock.TenantId == tenantId &&
                    productIds.Contains(
                        stock.ProductLocalId))
                .ToListAsync(cancellationToken);

        var stockByProductId =
            stocks
                .GroupBy(stock =>
                    stock.ProductLocalId)
                .ToDictionary(
                    group => group.Key,
                    group => group.First());

        return products
            .Select(product =>
            {
                stockByProductId.TryGetValue(
                    product.Id,
                    out var stock);

                return new LocalDamageProductResult
                {
                    ProductLocalId =
                        product.Id,

                    ProductServerId =
                        product.ServerId,

                    Name =
                        product.Name,

                    Barcode =
                        product.Barcode,

                    PurchasePrice =
                        product.PurchasePrice,

                    Quantity =
                        stock?.Quantity ?? 0m,

                    ReservedQuantity =
                        stock?.ReservedQuantity ?? 0m
                };
            })
            .ToList();
    }

    public async Task<IReadOnlyList<LocalDamageDraftResult>>
        GetDraftsAsync(
            CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var drafts =
            await _db.Damages
                .AsNoTracking()
                .Where(damage =>
                    damage.TenantId == tenantId &&
                    !damage.IsDeleted &&
                    damage.LocalStatus ==
                        LocalDamageStatus.Draft)
                .OrderByDescending(damage =>
                    damage.DamageDateUtc)
                .ToListAsync(cancellationToken);

        return drafts
            .Select(ToResult)
            .ToList();
    }

    public async Task<LocalDamageDraftResult> AddDraftAsync(
        Guid productLocalId,
        decimal quantity,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (productLocalId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "A product is required.");
        }

        if (quantity <= 0)
        {
            throw new InvalidOperationException(
                "Damage quantity must be greater than zero.");
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var product =
            await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == productLocalId &&
                        !item.IsDeletedLocally &&
                        item.IsActive,
                    cancellationToken);

        if (product == null)
        {
            throw new InvalidOperationException(
                "Product not found locally.");
        }

        var stock =
            await _db.Stocks
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.ProductLocalId ==
                            productLocalId,
                    cancellationToken);

        if (stock == null)
        {
            throw new InvalidOperationException(
                "No local stock was found for this product.");
        }

        var existingDraftQuantity =
            await _db.Damages
                .Where(damage =>
                    damage.TenantId == tenantId &&
                    damage.ProductLocalId ==
                        productLocalId &&
                    !damage.IsDeleted &&
                    damage.LocalStatus ==
                        LocalDamageStatus.Draft)
                .SumAsync(
                    damage => damage.Quantity,
                    cancellationToken);

        var availableQuantity =
            Math.Max(
                0,
                stock.Quantity -
                stock.ReservedQuantity -
                existingDraftQuantity);

        if (quantity > availableQuantity)
        {
            throw new InvalidOperationException(
                $"Only {availableQuantity:0.###} units are " +
                "available for damage registration.");
        }

        var now =
            DateTime.UtcNow;

        var damage =
            new LocalDamage
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ServerId = null,

                DamageNumber =
                    GenerateDamageNumber(),

                ProductLocalId =
                    product.Id,

                ProductServerId =
                    product.ServerId,

                ProductName =
                    product.Name,

                Quantity =
                    quantity,

                EstimatedValue =
                    Math.Round(
                        quantity *
                        product.PurchasePrice,
                        2),

                Reason =
                    NormalizeNullable(reason),

                DamageDateUtc =
                    now,

                LocalStatus =
                    LocalDamageStatus.Draft,

                SyncStatus =
                    SyncQueueStatus.Done,

                CreatedAtUtc =
                    now
            };

        _db.Damages.Add(damage);

        await _db.SaveChangesAsync(
            cancellationToken);

        return ToResult(damage);
    }

    public async Task RemoveDraftAsync(
        Guid localDamageId,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var damage =
            await _db.Damages
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.Id == localDamageId &&
                        item.LocalStatus ==
                            LocalDamageStatus.Draft,
                    cancellationToken);

        if (damage == null)
        {
            throw new InvalidOperationException(
                "Damage draft was not found.");
        }

        /*
         * Un vrai brouillon n'a pas encore été envoyé.
         * Il peut donc être supprimé physiquement.
         */
        _db.Damages.Remove(damage);

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<int> ValidateAllDraftsAsync(
     CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var drafts =
                await _db.Damages
                    .Where(damage =>
                        damage.TenantId == tenantId &&
                        !damage.IsDeleted &&
                        damage.LocalStatus ==
                            LocalDamageStatus.Draft)
                    .OrderBy(damage =>
                        damage.CreatedAtUtc)
                    .ToListAsync(cancellationToken);

            if (drafts.Count == 0)
            {
                await transaction.CommitAsync(
                    cancellationToken);

                return 0;
            }

            /*
             * Regrouper les brouillons par produit.
             *
             * Cela évite de vérifier et modifier plusieurs fois
             * le même stock.
             */
            var groupedQuantities =
                drafts
                    .GroupBy(damage =>
                        damage.ProductLocalId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Sum(
                            damage => damage.Quantity));

            var productIds =
                groupedQuantities.Keys.ToList();

            /*
             * ProductLocalId est obligatoire dans LocalStock.
             */
            var stocks =
                await _db.Stocks
                    .Where(stock =>
                        stock.TenantId == tenantId &&
                        productIds.Contains(
                            stock.ProductLocalId))
                    .ToListAsync(cancellationToken);

            var stocksByProduct =
                stocks
                    .GroupBy(stock =>
                        stock.ProductLocalId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.First());

            var products =
                await _db.Products
                    .Where(product =>
                        product.TenantId == tenantId &&
                        productIds.Contains(product.Id) &&
                        !product.IsDeletedLocally)
                    .ToDictionaryAsync(
                        product => product.Id,
                        cancellationToken);

            /*
             * Vérifier tous les stocks avant de modifier quoi que ce soit.
             */
            foreach (var groupedQuantity in groupedQuantities)
            {
                if (!stocksByProduct.TryGetValue(
                        groupedQuantity.Key,
                        out var stock))
                {
                    var productName =
                        products.TryGetValue(
                            groupedQuantity.Key,
                            out var missingStockProduct)
                            ? missingStockProduct.Name
                            : groupedQuantity.Key.ToString();

                    throw new InvalidOperationException(
                        $"No local stock was found for " +
                        $"'{productName}'.");
                }

                var availableQuantity =
                    Math.Max(
                        0m,
                        stock.Quantity -
                        stock.ReservedQuantity);

                if (groupedQuantity.Value >
                    availableQuantity)
                {
                    var productName =
                        products.TryGetValue(
                            groupedQuantity.Key,
                            out var product)
                            ? product.Name
                            : groupedQuantity.Key.ToString();

                    throw new InvalidOperationException(
                        $"Insufficient stock for '{productName}'. " +
                        $"Available: {availableQuantity:0.###}; " +
                        $"requested: {groupedQuantity.Value:0.###}.");
                }
            }

            var draftIds =
                drafts
                    .Select(damage => damage.Id)
                    .ToList();

            var existingQueueIds =
                await _db.SyncQueueItems
                    .AsNoTracking()
                    .Where(item =>
                        item.TenantId == tenantId &&
                        item.EntityName ==
                            DamageEntityName &&
                        draftIds.Contains(
                            item.LocalEntityId) &&
                        item.Status !=
                            SyncQueueStatus.Done)
                    .Select(item =>
                        item.LocalEntityId)
                    .ToListAsync(cancellationToken);

            var existingQueueSet =
                existingQueueIds.ToHashSet();

            var now =
                DateTime.UtcNow;

            /*
             * Réduire le stock local immédiatement.
             */
            foreach (var groupedQuantity in groupedQuantities)
            {
                var stock =
                    stocksByProduct[
                        groupedQuantity.Key];

                stock.Quantity -=
                    groupedQuantity.Value;

                stock.LastUpdatedUtc =
                    now;

                /*
                 * Le stock contient maintenant une modification
                 * locale non encore confirmée par le serveur.
                 */
                if (products.TryGetValue(
                        groupedQuantity.Key,
                        out var product))
                {
                    product.LocalStockQuantity =
                        stock.Quantity;

                    product.ModifiedAtUtc =
                        now;
                }
            }

            /*
             * Transformer les brouillons en opérations à synchroniser.
             */
            foreach (var damage in drafts)
            {
                damage.LocalStatus =
                    LocalDamageStatus.Pending;

                damage.SyncStatus =
                    SyncQueueStatus.Pending;

                damage.ValidatedAtUtc =
                    now;

                damage.ModifiedAtUtc =
                    now;

                if (existingQueueSet.Contains(
                        damage.Id))
                {
                    continue;
                }

                _db.SyncQueueItems.Add(
                    new SyncQueueItem
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,

                        ClientOperationId =
                            Guid.NewGuid(),

                        LocalEntityId =
                            damage.Id,

                        EntityName =
                            DamageEntityName,

                        Operation =
                            SyncOperation.Create,

                        Status =
                            SyncQueueStatus.Pending,

                        Attempts = 0,
                        CreatedAtUtc = now
                    });
            }

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return drafts.Count;
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    private static LocalDamageDraftResult ToResult(
        LocalDamage damage)
    {
        return new LocalDamageDraftResult
        {
            Id = damage.Id,
            DamageNumber = damage.DamageNumber,
            ProductLocalId = damage.ProductLocalId,
            ProductServerId = damage.ProductServerId,
            ProductName = damage.ProductName,
            Quantity = damage.Quantity,
            EstimatedValue = damage.EstimatedValue,
            Reason = damage.Reason,
            DamageDateUtc = damage.DamageDateUtc
        };
    }

    private static string GenerateDamageNumber()
    {
        return
            $"DMG-{DateTime.UtcNow:yyyyMMddHHmmssfff}-" +
            $"{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private static string? NormalizeNullable(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}