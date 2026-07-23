using Inventory.Dto.Enums;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services;

public sealed class LocalProductService : ILocalProductService
{
    private const string ProductEntityName = "Product";

    private readonly PosLocalDbContext _db;
    private readonly ILocalTenantContext _tenantContext;

    public LocalProductService(
        PosLocalDbContext db,
        ILocalTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<ProductResult> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CatalogProductId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Catalog product is required.");
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var catalog = await _db.ProductCatalogs
            .AsNoTracking()
            .Include(x => x.PackComponents)
            .FirstOrDefaultAsync(
                x =>
                    x.Id == request.CatalogProductId &&
                    !x.IsDeleted,
                cancellationToken);

        if (catalog == null)
        {
            throw new InvalidOperationException(
                "The selected product catalog does not exist locally.");
        }

        var alreadyExists = await _db.Products
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.CatalogProductId == request.CatalogProductId &&
                    !x.IsDeletedLocally,
                cancellationToken);

        if (alreadyExists)
        {
            throw new InvalidOperationException(
                $"'{catalog.Name}' is already activated for this tenant.");
        }

        var packComponent =
            catalog.PackComponents?
                .FirstOrDefault();

        var now = DateTime.UtcNow;

        var product = new LocalProduct
        {
            Id = Guid.NewGuid(),
            ServerId = null,

            TenantId = tenantId,

            CatalogProductId =
                request.CatalogProductId,

            Name = string.IsNullOrWhiteSpace(catalog.Name)
                ? "Unnamed product"
                : catalog.Name.Trim(),

            Sku = string.IsNullOrWhiteSpace(catalog.InternalCode)
                ? null
                : catalog.InternalCode.Trim(),

            Barcode = string.IsNullOrWhiteSpace(catalog.Barcode)
                ? null
                : catalog.Barcode.Trim(),

            Brand = string.IsNullOrWhiteSpace(catalog.Brand)
                ? null
                : catalog.Brand.Trim(),

            Unit = string.IsNullOrWhiteSpace(catalog.UnitOfMeasure)
                ? null
                : catalog.UnitOfMeasure.Trim(),

            SalePrice = request.SalePrice,
            SalePrice2 = request.SalePrice2,
            SalePrice3 = request.SalePrice3,
            PurchasePrice = request.PurchasePrice,
            VatRate = request.VatRate,

            MinStockLevel = request.MinStockLevel,
            MaxStockLevel = request.MaxStockLevel,

            IsTracked = request.IsTracked,

            Status = request.IsActive,

            IsActive =
                request.IsActive == ProductStatus.Active,

            IsPack = catalog.IsPack,

            UnitsPerPack =
                catalog.IsPack
                    ? packComponent?.Quantity ?? 1
                    : 1,

            LocalStockQuantity = 0,

            IsDeletedLocally = false,

            SyncStatus =
                SyncQueueStatus.Pending,

            CreatedAtUtc = now
        };

        await _db.Products.AddAsync(
            product,
            cancellationToken);

        await EnsureQueueItemAsync(
            tenantId,
            product.Id,
            SyncOperation.Create,
            cancellationToken);

        await _db.SaveChangesAsync(
            cancellationToken);

        return ToResult(product);
    }

    public async Task<ProductResult> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Product ID cannot be empty.",
                nameof(id));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var product = await _db.Products
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.TenantId == tenantId &&
                    !x.IsDeletedLocally,
                cancellationToken);

        if (product == null)
        {
            throw new InvalidOperationException(
                "Local product not found.");
        }

        product.SalePrice = request.SalePrice;
        product.SalePrice2 = request.SalePrice2;
        product.SalePrice3 = request.SalePrice3;
        product.PurchasePrice = request.PurchasePrice;
        product.VatRate = request.VatRate;

        product.MinStockLevel =
            request.MinStockLevel;

        product.MaxStockLevel =
            request.MaxStockLevel;

        product.IsTracked =
            request.IsTracked;

        product.Status =
            request.IsActive;

        product.IsActive =
            request.IsActive == ProductStatus.Active;

        product.ModifiedAtUtc =
            DateTime.UtcNow;

        product.SyncStatus =
            SyncQueueStatus.Pending;

        if (product.ServerId.HasValue &&
            product.ServerId.Value != Guid.Empty)
        {
            await EnsureQueueItemAsync(
                tenantId,
                product.Id,
                SyncOperation.Update,
                cancellationToken);
        }
        else
        {
            await EnsureQueueItemAsync(
                tenantId,
                product.Id,
                SyncOperation.Create,
                cancellationToken);
        }

        await _db.SaveChangesAsync(
            cancellationToken);

        return ToResult(product);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Product ID cannot be empty.",
                nameof(id));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var product = await _db.Products
            .FirstOrDefaultAsync(
                x =>
                    x.Id == id &&
                    x.TenantId == tenantId &&
                    !x.IsDeletedLocally,
                cancellationToken);

        if (product == null)
        {
            throw new InvalidOperationException(
                "Local product not found.");
        }

        var now = DateTime.UtcNow;

        product.IsDeletedLocally = true;
        product.DeletedAtUtc = now;
        product.ModifiedAtUtc = now;

        if (product.ServerId.HasValue &&
            product.ServerId.Value != Guid.Empty)
        {
            product.SyncStatus =
                SyncQueueStatus.Pending;

            await MarkOtherOperationsAsCompletedAsync(
                tenantId,
                product.Id,
                SyncOperation.Delete,
                "Superseded by product deletion.",
                cancellationToken);

            await EnsureQueueItemAsync(
                tenantId,
                product.Id,
                SyncOperation.Delete,
                cancellationToken);
        }
        else
        {
            /*
             * Le produit n'existe pas encore sur le serveur.
             * La création en attente doit simplement être annulée.
             */
            product.SyncStatus =
                SyncQueueStatus.Done;

            await MarkOtherOperationsAsCompletedAsync(
                tenantId,
                product.Id,
                operationToKeep: null,
                reason:
                    "Local-only product deleted before synchronization.",
                cancellationToken);
        }

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<PagedResult<ProductResult>> QueryAsync(
        ProductQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var page =
            query.Page <= 0 ? 1 : query.Page;

        var pageSize =
            query.PageSize <= 0 ? 10 : query.PageSize;

        var productsQuery = _db.Products
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                !x.IsDeletedLocally);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term =
                query.Search.Trim();

            productsQuery = productsQuery.Where(x =>
                x.Name.Contains(term) ||
                (x.Barcode != null &&
                 x.Barcode.Contains(term)) ||
                (x.Sku != null &&
                 x.Sku.Contains(term)) ||
                (x.Brand != null &&
                 x.Brand.Contains(term)) ||
                (x.Category != null &&
                 x.Category.Contains(term)));
        }

        var totalCount =
            await productsQuery.CountAsync(
                cancellationToken);

        var items = await productsQuery
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Barcode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToResult(x))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResult>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<ProductResult>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        return await _db.Products
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                !x.IsDeletedLocally)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Barcode)
            .Select(x => ToResult(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<LocalProduct?> GetByBarcodeAsync(
        string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var normalized =
            barcode.Trim();

        return await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.Barcode == normalized &&
                x.Status == ProductStatus.Active &&
                !x.IsDeletedLocally);
    }

    public async Task<LocalProductScanResult?> ResolveBarcodeAsync(
        string barcode)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var product =
            await GetByBarcodeAsync(barcode);

        if (product == null)
            return null;

        if (!product.IsPack)
        {
            return new LocalProductScanResult
            {
                ProductLocalId = product.Id,
                ProductServerId = product.ServerId,
                ProductName = product.Name,
                ProductBarcode = product.Barcode,

                UnitProductLocalId = product.Id,
                UnitProductServerId = product.ServerId,
                UnitProductName = product.Name,
                UnitProductBarcode = product.Barcode,

                IsPack = false,

                Quantity = 1,
                UnitQuantity = 1,

                UnitPrice = product.SalePrice,
                PurchasePrice = product.PurchasePrice,
                VatRate = product.VatRate
            };
        }

        LocalProduct? unitProduct = null;

        if (product.UnitProductLocalId.HasValue)
        {
            unitProduct = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == product.UnitProductLocalId.Value &&
                        x.TenantId == tenantId &&
                        x.Status == ProductStatus.Active &&
                        !x.IsDeletedLocally);
        }

        if (unitProduct == null &&
            product.UnitProductServerId.HasValue)
        {
            unitProduct = await _db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.ServerId ==
                            product.UnitProductServerId.Value &&
                        x.TenantId == tenantId &&
                        x.Status == ProductStatus.Active &&
                        !x.IsDeletedLocally);
        }

        if (unitProduct == null)
        {
            throw new InvalidOperationException(
                $"The unit product for pack '{product.Name}' " +
                "was not found locally.");
        }

        return new LocalProductScanResult
        {
            ProductLocalId = product.Id,
            ProductServerId = product.ServerId,
            ProductName = product.Name,
            ProductBarcode = product.Barcode,

            UnitProductLocalId = unitProduct.Id,
            UnitProductServerId = unitProduct.ServerId,
            UnitProductName = unitProduct.Name,
            UnitProductBarcode = unitProduct.Barcode,

            IsPack = true,

            Quantity = 1,

            UnitQuantity =
                product.UnitsPerPack <= 0
                    ? 1
                    : product.UnitsPerPack,

            UnitPrice = product.SalePrice,
            PurchasePrice = unitProduct.PurchasePrice,
            VatRate = product.VatRate
        };
    }

    public async Task UpsertAsync(
        LocalProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!product.ServerId.HasValue ||
            product.ServerId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Cannot upsert a server product without ServerId.");
        }

        var currentTenantId =
            _tenantContext.GetRequiredTenantId();

        var tenantId =
            product.TenantId == Guid.Empty
                ? currentTenantId
                : product.TenantId;

        if (tenantId != currentTenantId)
        {
            throw new InvalidOperationException(
                "Cannot upsert a product belonging to another tenant.");
        }

        LocalProduct? existing = null;

        if (product.Id != Guid.Empty)
        {
            existing = await _db.Products
                .FirstOrDefaultAsync(x =>
                    x.Id == product.Id &&
                    x.TenantId == tenantId);
        }

        existing ??= await _db.Products
            .FirstOrDefaultAsync(x =>
                x.ServerId == product.ServerId &&
                x.TenantId == tenantId);

        if (existing == null)
        {
            product.Id =
                product.Id == Guid.Empty
                    ? Guid.NewGuid()
                    : product.Id;

            product.TenantId = tenantId;
            product.IsDeletedLocally = false;
            product.SyncStatus = SyncQueueStatus.Done;
            product.LastSyncedAtUtc = DateTime.UtcNow;

            await _db.Products.AddAsync(product);
        }
        else
        {
            MapServerProduct(
                product,
                existing);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<LocalProduct>> SearchAsync(
        string search,
        int take = 50)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var safeTake =
            Math.Clamp(take, 1, 200);

        var query = _db.Products
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.Status == ProductStatus.Active &&
                !x.IsDeletedLocally);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term =
                search.Trim();

            query = query.Where(x =>
                x.Name.Contains(term) ||
                (x.Barcode != null &&
                 x.Barcode.Contains(term)) ||
                (x.Sku != null &&
                 x.Sku.Contains(term)));
        }

        return await query
            .OrderBy(x => x.Name)
            .Take(safeTake)
            .ToListAsync();
    }

    private async Task EnsureQueueItemAsync(
        Guid tenantId,
        Guid localEntityId,
        string operation,
        CancellationToken cancellationToken)
    {
        /*
         * Une modification d'un produit qui possède encore une création
         * Pending n'a pas besoin d'un élément Update supplémentaire.
         * La création utilisera les dernières valeurs SQLite.
         */
        if (operation == SyncOperation.Update)
        {
            var pendingCreate =
                await _db.SyncQueueItems.AnyAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.LocalEntityId == localEntityId &&
                        x.EntityName == ProductEntityName &&
                        x.Operation == SyncOperation.Create &&
                        x.Status != SyncQueueStatus.Done,
                    cancellationToken);

            if (pendingCreate)
                return;
        }

        var existing =
            await _db.SyncQueueItems
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.LocalEntityId == localEntityId &&
                        x.EntityName == ProductEntityName &&
                        x.Operation == operation &&
                        x.Status != SyncQueueStatus.Done,
                    cancellationToken);

        if (existing != null)
        {
            existing.Status =
                SyncQueueStatus.Pending;

            existing.ErrorMessage = null;
            existing.ProcessedAtUtc = null;

            return;
        }

        await _db.SyncQueueItems.AddAsync(
            new SyncQueueItem
            {
                Id = Guid.NewGuid(),

                TenantId = tenantId,

                ClientOperationId =
                    Guid.NewGuid(),

                LocalEntityId =
                    localEntityId,

                EntityName =
                    ProductEntityName,

                Operation =
                    operation,

                Status =
                    SyncQueueStatus.Pending,

                Attempts = 0,

                CreatedAtUtc =
                    DateTime.UtcNow
            },
            cancellationToken);
    }

    private async Task MarkOtherOperationsAsCompletedAsync(
        Guid tenantId,
        Guid localEntityId,
        string? operationToKeep,
        string reason,
        CancellationToken cancellationToken)
    {
        var query = _db.SyncQueueItems
            .Where(x =>
                x.TenantId == tenantId &&
                x.LocalEntityId == localEntityId &&
                x.EntityName == ProductEntityName &&
                x.Status != SyncQueueStatus.Done);

        if (!string.IsNullOrWhiteSpace(operationToKeep))
        {
            query = query.Where(
                x => x.Operation != operationToKeep);
        }

        var queueItems =
            await query.ToListAsync(
                cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var queueItem in queueItems)
        {
            queueItem.Status =
                SyncQueueStatus.Done;

            queueItem.ProcessedAtUtc =
                now;

            queueItem.ErrorMessage =
                reason;
        }
    }

    private static void MapServerProduct(
        LocalProduct source,
        LocalProduct destination)
    {
        destination.ServerId =
            source.ServerId;

        destination.CatalogProductId =
            source.CatalogProductId;

        destination.Name =
            source.Name;

        destination.Sku =
            source.Sku;

        destination.Barcode =
            source.Barcode;

        destination.Category =
            source.Category;

        destination.Brand =
            source.Brand;

        destination.SalePrice =
            source.SalePrice;

        destination.SalePrice2 =
            source.SalePrice2;

        destination.SalePrice3 =
            source.SalePrice3;

        destination.PurchasePrice =
            source.PurchasePrice;

        destination.VatRate =
            source.VatRate;

        destination.MinStockLevel =
            source.MinStockLevel;

        destination.MaxStockLevel =
            source.MaxStockLevel;

        destination.Unit =
            source.Unit;

        destination.Status =
            source.Status;

        destination.IsActive =
            source.Status == ProductStatus.Active;

        destination.IsTracked =
            source.IsTracked;

        destination.LocalStockQuantity =
            source.LocalStockQuantity;

        destination.IsPack =
            source.IsPack;

        destination.UnitProductLocalId =
            source.UnitProductLocalId;

        destination.UnitProductServerId =
            source.UnitProductServerId;

        destination.UnitsPerPack =
            source.UnitsPerPack;

        destination.IsDeletedLocally =
            false;

        destination.SyncStatus =
            SyncQueueStatus.Done;

        destination.ServerModifiedAtUtc =
            source.ServerModifiedAtUtc;

        destination.LastSyncedAtUtc =
            DateTime.UtcNow;
    }

    private static ProductResult ToResult(
        LocalProduct product)
    {
        return new ProductResult
        {
            /*
             * L'écran local continue à recevoir l'ID SQLite.
             * ServerId sera utilisé explicitement par les services réseau.
             */
            Id = product.Id,

            CatalogProductId =
                product.CatalogProductId ?? Guid.Empty,

            CatalogName =
                product.Name,

            CatalogBrand =
                product.Brand,

            CatalogBarcode =
                product.Barcode,

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

            Status =
                product.Status,

            IsTracked =
                product.IsTracked,

            IsPack =
                product.IsPack,

            PackSize =
                product.UnitsPerPack,

            ComponentProductId =
                product.UnitProductServerId
        };
    }
}