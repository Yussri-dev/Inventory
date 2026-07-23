using Inventory.Dto.Enums;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services;

public sealed class LocalProductSyncService
    : ILocalProductSyncService
{
    private const string ProductEntityName = "Product";
    private const string FullSyncMode = "Full";

    private const int PageSize = 100;
    private const int MaximumPages = 10_000;

    private readonly PosLocalDbContext _db;
    private readonly IProductApi _productApi;
    private readonly ILocalTenantContext _tenantContext;
    private readonly ILogger<LocalProductSyncService> _logger;

    public LocalProductSyncService(
        PosLocalDbContext db,
        IProductApi productApi,
        ILocalTenantContext tenantContext,
        ILogger<LocalProductSyncService> logger)
    {
        _db = db;
        _productApi = productApi;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task FullSyncAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        try
        {
            /*
             * Les appels HTTP sont terminés avant d'ouvrir
             * la transaction SQLite.
             */
            var serverProducts =
                await DownloadAllServerProductsAsync(
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var now =
                    DateTime.UtcNow;

                /*
                 * Produits avec une modification locale non envoyée.
                 *
                 * Une synchronisation descendante ne doit pas
                 * écraser une création, modification ou suppression
                 * locale en attente.
                 */
                var pendingLocalIds =
                    await GetPendingProductIdsAsync(
                        tenantId,
                        cancellationToken);

                /*
                 * Les catalogues doivent avoir été synchronisés
                 * avant les Product du tenant.
                 */
                var catalogs =
                    await _db.ProductCatalogs
                        .AsNoTracking()
                        .Where(x => !x.IsDeleted)
                        .ToDictionaryAsync(
                            x => x.Id,
                            cancellationToken);

                var categories =
                    await _db.ProductCategories
                        .AsNoTracking()
                        .Where(x => !x.IsDeleted)
                        .ToDictionaryAsync(
                            x => x.Id,
                            x => x.Name,
                            cancellationToken);

                var localProducts =
                    await _db.Products
                        .Where(x => x.TenantId == tenantId)
                        .ToListAsync(cancellationToken);

                var localById =
                    localProducts.ToDictionary(
                        x => x.Id);

                var localByServerId =
                    localProducts
                        .Where(x =>
                            x.ServerId.HasValue &&
                            x.ServerId.Value != Guid.Empty)
                        .GroupBy(x => x.ServerId!.Value)
                        .ToDictionary(
                            x => x.Key,
                            x => x.First());

                var localByCatalogId =
                    localProducts
                        .Where(x =>
                            x.CatalogProductId.HasValue &&
                            x.CatalogProductId.Value != Guid.Empty &&
                            !x.IsDeletedLocally)
                        .GroupBy(x => x.CatalogProductId!.Value)
                        .ToDictionary(
                            x => x.Key,
                            x => x.First());

                foreach (var serverProduct in serverProducts)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    ValidateServerProduct(
                        serverProduct);

                    if (!catalogs.TryGetValue(
                            serverProduct.CatalogProductId,
                            out var catalog))
                    {
                        throw new InvalidOperationException(
                            $"Catalog product " +
                            $"'{serverProduct.CatalogProductId}' " +
                            $"does not exist in SQLite. " +
                            $"Synchronize ProductCatalog before Product.");
                    }

                    LocalProduct? localProduct = null;

                    /*
                     * Correspondance principale :
                     * TenantId + ServerId.
                     */
                    if (localByServerId.TryGetValue(
                            serverProduct.Id,
                            out var byServer))
                    {
                        localProduct = byServer;
                    }
                    /*
                     * Correspondance secondaire :
                     * TenantId + CatalogProductId.
                     *
                     * Ce cas permet notamment de réconcilier une
                     * ancienne ligne locale n'ayant pas encore
                     * reçu son ServerId.
                     */
                    else if (localByCatalogId.TryGetValue(
                                 serverProduct.CatalogProductId,
                                 out var byCatalog))
                    {
                        localProduct = byCatalog;
                    }

                    /*
                     * Protéger les modifications offline.
                     */
                    if (localProduct != null &&
                        pendingLocalIds.Contains(localProduct.Id))
                    {
                        _logger.LogDebug(
                            "Skipping server Product {ServerId} because " +
                            "local Product {LocalId} has pending changes.",
                            serverProduct.Id,
                            localProduct.Id);

                        continue;
                    }

                    if (localProduct == null)
                    {
                        localProduct = new LocalProduct
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            CreatedAtUtc = now
                        };

                        _db.Products.Add(localProduct);
                        localProducts.Add(localProduct);

                        localById[localProduct.Id] =
                            localProduct;
                    }

                    /*
                     * Une même activation ProductCatalog ne peut pas
                     * représenter deux Product serveur différents
                     * dans le même tenant.
                     */
                    if (localProduct.ServerId.HasValue &&
                        localProduct.ServerId.Value != Guid.Empty &&
                        localProduct.ServerId.Value != serverProduct.Id)
                    {
                        throw new InvalidOperationException(
                            $"Local Product '{localProduct.Id}' is already " +
                            $"linked to server Product " +
                            $"'{localProduct.ServerId.Value}', but the server " +
                            $"returned Product '{serverProduct.Id}' for the " +
                            $"same catalog '{serverProduct.CatalogProductId}'.");
                    }

                    categories.TryGetValue(
                        catalog.CategoryId,
                        out var categoryName);

                    ApplyServerProduct(
                        localProduct,
                        serverProduct,
                        catalog,
                        categoryName,
                        tenantId,
                        now);

                    localByServerId[serverProduct.Id] =
                        localProduct;

                    localByCatalogId[
                        serverProduct.CatalogProductId] =
                        localProduct;
                }

                /*
                 * ComponentProductId contient un ServerId.
                 * Après avoir inséré tous les produits, nous pouvons
                 * résoudre le LocalProduct correspondant.
                 */
                ResolvePackLocalProductIds(
                    localProducts);

                await MarkSyncSucceededAsync(
                    tenantId,
                    now,
                    cancellationToken);

                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Product synchronization completed for tenant {TenantId}. " +
                    "{Count} server products were processed.",
                    tenantId,
                    serverProducts.Count);
            }
            catch
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            await RecordSyncFailureAsync(
                tenantId,
                exception);

            _logger.LogError(
                exception,
                "Product synchronization failed for tenant {TenantId}.",
                tenantId);

            throw;
        }
    }

    public async Task UpsertFromServerAsync(
        ProductResult serverProduct,
        Guid? originatingLocalId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateServerProduct(
            serverProduct);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var now =
                DateTime.UtcNow;

            var catalog =
                await _db.ProductCatalogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.Id == serverProduct.CatalogProductId &&
                            !x.IsDeleted,
                        cancellationToken);

            if (catalog == null)
            {
                throw new InvalidOperationException(
                    $"Catalog product " +
                    $"'{serverProduct.CatalogProductId}' " +
                    $"was not found locally.");
            }

            string? categoryName = null;

            if (catalog.CategoryId != Guid.Empty)
            {
                categoryName =
                    await _db.ProductCategories
                        .AsNoTracking()
                        .Where(x =>
                            x.Id == catalog.CategoryId &&
                            !x.IsDeleted)
                        .Select(x => x.Name)
                        .FirstOrDefaultAsync(
                            cancellationToken);
            }

            LocalProduct? localProduct = null;

            /*
             * Pour une création offline envoyée au serveur,
             * originatingLocalId permet de conserver le même Id local.
             */
            if (originatingLocalId.HasValue &&
                originatingLocalId.Value != Guid.Empty)
            {
                localProduct =
                    await _db.Products
                        .FirstOrDefaultAsync(
                            x =>
                                x.TenantId == tenantId &&
                                x.Id == originatingLocalId.Value,
                            cancellationToken);
            }

            localProduct ??=
                await _db.Products
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.ServerId == serverProduct.Id,
                        cancellationToken);

            localProduct ??=
                await _db.Products
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.CatalogProductId ==
                                serverProduct.CatalogProductId &&
                            !x.IsDeletedLocally,
                        cancellationToken);

            /*
             * Un pull normal ne doit pas écraser une modification
             * locale en attente.
             *
             * Lorsqu'originatingLocalId existe, la réponse vient
             * justement de l'envoi de cette modification.
             */
            if (localProduct != null &&
                !originatingLocalId.HasValue)
            {
                var hasPendingOperation =
                    await _db.SyncQueueItems
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.TenantId == tenantId &&
                                x.EntityName ==
                                    ProductEntityName &&
                                x.LocalEntityId ==
                                    localProduct.Id &&
                                x.Status !=
                                    SyncQueueStatus.Done,
                            cancellationToken);

                if (hasPendingOperation)
                {
                    _logger.LogDebug(
                        "Server Product {ServerId} was not applied because " +
                        "local Product {LocalId} has pending changes.",
                        serverProduct.Id,
                        localProduct.Id);

                    await transaction.CommitAsync(
                        cancellationToken);

                    return;
                }
            }

            if (localProduct == null)
            {
                localProduct = new LocalProduct
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CreatedAtUtc = now
                };

                _db.Products.Add(
                    localProduct);
            }

            if (localProduct.ServerId.HasValue &&
                localProduct.ServerId.Value != Guid.Empty &&
                localProduct.ServerId.Value != serverProduct.Id)
            {
                throw new InvalidOperationException(
                    $"Local Product '{localProduct.Id}' is linked to " +
                    $"another server Product.");
            }

            ApplyServerProduct(
                localProduct,
                serverProduct,
                catalog,
                categoryName,
                tenantId,
                now);

            await ResolveSinglePackLocalProductIdAsync(
                localProduct,
                tenantId,
                cancellationToken);

            /*
             * La réponse serveur confirme que l'opération locale
             * a été enregistrée.
             */
            if (originatingLocalId.HasValue)
            {
                await CompleteQueueItemsAsync(
                    tenantId,
                    localProduct.Id,
                    now,
                    cancellationToken);
            }

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    public async Task MarkDeletedFromServerAsync(
        Guid serverProductId,
        CancellationToken cancellationToken = default)
    {
        if (serverProductId == Guid.Empty)
        {
            throw new ArgumentException(
                "Server product id is required.",
                nameof(serverProductId));
        }

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        var localProduct =
            await _db.Products
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.ServerId == serverProductId,
                    cancellationToken);

        if (localProduct == null)
            return;

        var now =
            DateTime.UtcNow;

        localProduct.IsDeletedLocally = true;
        localProduct.IsActive = false;
        localProduct.DeletedAtUtc = now;
        localProduct.ModifiedAtUtc = now;
        localProduct.LastSyncedAtUtc = now;
        localProduct.SyncStatus = SyncQueueStatus.Done;

        await CompleteQueueItemsAsync(
            tenantId,
            localProduct.Id,
            now,
            cancellationToken);

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<bool> HasInitialSyncCompletedAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        return await _db.SyncTableStates
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.TenantId == tenantId &&
                    x.EntityName == ProductEntityName &&
                    x.InitialSyncCompleted,
                cancellationToken);
    }

    private async Task<List<ProductResult>> DownloadAllServerProductsAsync(
            CancellationToken cancellationToken)
    {
        var products =
            new List<ProductResult>();

        for (var page = 1;
             page <= MaximumPages;
             page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var query =
                new ProductQuery
                {
                    Page = page,
                    PageSize = PageSize,
                    SortBy = "name",
                    Desc = false
                };

            var response =
                await _productApi.Search(
                    query,
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var pageItems =
                response.Items?.ToList()
                ?? new List<ProductResult>();

            if (pageItems.Count == 0)
            {
                break;
            }

            products.AddRange(pageItems);

            if (response.TotalCount > 0 &&
                products.Count >= response.TotalCount)
            {
                break;
            }

            if (pageItems.Count < PageSize)
            {
                break;
            }

            if (page == MaximumPages)
            {
                throw new InvalidOperationException(
                    "Product synchronization exceeded the maximum " +
                    "number of pages.");
            }
        }

        return products
            .Where(x => x.Id != Guid.Empty)
            .GroupBy(x => x.Id)
            .Select(group => group.Last())
            .ToList();
    }

    private async Task<HashSet<Guid>>
        GetPendingProductIdsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        var ids =
            await _db.SyncQueueItems
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.EntityName == ProductEntityName &&
                    x.Status != SyncQueueStatus.Done)
                .Select(x => x.LocalEntityId)
                .Distinct()
                .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    private static void ApplyServerProduct(
        LocalProduct localProduct,
        ProductResult serverProduct,
        LocalProductCatalog catalog,
        string? categoryName,
        Guid tenantId,
        DateTime now)
    {
        localProduct.TenantId =
            tenantId;

        localProduct.ServerId =
            serverProduct.Id;

        localProduct.CatalogProductId =
            serverProduct.CatalogProductId;

        localProduct.Name =
            FirstNotEmpty(
                serverProduct.CatalogName,
                catalog.Name,
                $"Product {serverProduct.CatalogProductId}");

        localProduct.Sku =
            NullIfWhiteSpace(
                catalog.InternalCode);

        localProduct.Barcode =
            FirstNotEmptyOrNull(
                serverProduct.CatalogBarcode,
                catalog.Barcode);

        localProduct.Brand =
            FirstNotEmptyOrNull(
                serverProduct.CatalogBrand,
                catalog.Brand);

        localProduct.Category =
            NullIfWhiteSpace(
                categoryName);

        localProduct.Unit =
            string.IsNullOrWhiteSpace(
                catalog.UnitOfMeasure)
                ? "pcs"
                : catalog.UnitOfMeasure.Trim();

        localProduct.SalePrice =
            serverProduct.SalePrice;

        localProduct.SalePrice2 =
            serverProduct.SalePrice2;

        localProduct.SalePrice3 =
            serverProduct.SalePrice3;

        localProduct.PurchasePrice =
            serverProduct.PurchasePrice;

        localProduct.VatRate =
            serverProduct.VatRate;

        localProduct.MinStockLevel =
            serverProduct.MinStockLevel;

        localProduct.MaxStockLevel =
            serverProduct.MaxStockLevel;

        localProduct.Status =
            serverProduct.Status;

        localProduct.IsActive =
            serverProduct.Status ==
            ProductStatus.Active;

        localProduct.IsTracked =
            serverProduct.IsTracked;

        localProduct.IsPack =
            serverProduct.IsPack ||
            catalog.IsPack;

        localProduct.UnitProductServerId =
            NormalizeGuid(
                serverProduct.ComponentProductId);

        localProduct.UnitsPerPack =
            serverProduct.PackSize > 0
                ? serverProduct.PackSize
                : 1m;

        localProduct.IsDeletedLocally =
            false;

        localProduct.DeletedAtUtc =
            null;

        localProduct.SyncStatus =
            SyncQueueStatus.Done;

        localProduct.LastSyncedAtUtc =
            now;

        localProduct.ModifiedAtUtc =
            now;

        if (localProduct.CreatedAtUtc == default)
        {
            localProduct.CreatedAtUtc =
                now;
        }
    }

    private static void ResolvePackLocalProductIds(
        IEnumerable<LocalProduct> products)
    {
        var productsByServerId =
            products
                .Where(x =>
                    x.ServerId.HasValue &&
                    x.ServerId.Value != Guid.Empty)
                .GroupBy(x => x.ServerId!.Value)
                .ToDictionary(
                    x => x.Key,
                    x => x.First());

        foreach (var product in products)
        {
            if (!product.UnitProductServerId.HasValue ||
                product.UnitProductServerId.Value ==
                Guid.Empty)
            {
                product.UnitProductLocalId =
                    null;

                continue;
            }

            product.UnitProductLocalId =
                productsByServerId.TryGetValue(
                    product.UnitProductServerId.Value,
                    out var unitProduct)
                    ? unitProduct.Id
                    : null;
        }
    }

    private async Task ResolveSinglePackLocalProductIdAsync(
        LocalProduct product,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!product.UnitProductServerId.HasValue ||
            product.UnitProductServerId.Value == Guid.Empty)
        {
            product.UnitProductLocalId =
                null;

            return;
        }

        product.UnitProductLocalId =
            await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ServerId ==
                        product.UnitProductServerId.Value)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(
                    cancellationToken);
    }

    private async Task CompleteQueueItemsAsync(
        Guid tenantId,
        Guid localProductId,
        DateTime processedAtUtc,
        CancellationToken cancellationToken)
    {
        var queueItems =
            await _db.SyncQueueItems
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.EntityName == ProductEntityName &&
                    x.LocalEntityId == localProductId &&
                    x.Status != SyncQueueStatus.Done)
                .ToListAsync(cancellationToken);

        foreach (var queueItem in queueItems)
        {
            queueItem.Status =
                SyncQueueStatus.Done;

            queueItem.ProcessedAtUtc =
                processedAtUtc;

            queueItem.ErrorMessage =
                null;
        }
    }

    private async Task MarkSyncSucceededAsync(
        Guid tenantId,
        DateTime synchronizedAtUtc,
        CancellationToken cancellationToken)
    {
        var state =
            await _db.SyncTableStates
                .FirstOrDefaultAsync(
                    x =>
                        x.TenantId == tenantId &&
                        x.EntityName ==
                            ProductEntityName,
                    cancellationToken);

        if (state == null)
        {
            state = new SyncTableStateLocal
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EntityName = ProductEntityName,
                Syncmode = FullSyncMode
            };

            _db.SyncTableStates.Add(
                state);
        }

        state.Syncmode =
            FullSyncMode;

        state.InitialSyncCompleted =
            true;

        state.LastSuccessfulSyncAtUtc =
            synchronizedAtUtc;

        state.LastError =
            null;

        state.ContinuationToken =
            null;
    }

    private async Task RecordSyncFailureAsync(
        Guid tenantId,
        Exception exception)
    {
        try
        {
            /*
             * Une transaction annulée peut laisser des entités
             * modifiées dans le ChangeTracker.
             */
            _db.ChangeTracker.Clear();

            var state =
                await _db.SyncTableStates
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.EntityName ==
                                ProductEntityName,
                        CancellationToken.None);

            if (state == null)
            {
                state = new SyncTableStateLocal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    EntityName = ProductEntityName,
                    Syncmode = FullSyncMode,
                    InitialSyncCompleted = false
                };

                _db.SyncTableStates.Add(
                    state);
            }

            state.LastError =
                Truncate(
                    exception.GetBaseException().Message,
                    2000);

            await _db.SaveChangesAsync(
                CancellationToken.None);
        }
        catch (Exception stateException)
        {
            _logger.LogWarning(
                stateException,
                "Could not persist Product sync failure state " +
                "for tenant {TenantId}.",
                tenantId);
        }
    }

    private static void ValidateServerProduct(
        ProductResult product)
    {
        ArgumentNullException.ThrowIfNull(
            product);

        if (product.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The server returned a Product without an Id.");
        }

        if (product.CatalogProductId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Server Product '{product.Id}' does not contain " +
                $"a CatalogProductId.");
        }

        if (product.SalePrice < 0 ||
            product.SalePrice2 < 0 ||
            product.SalePrice3 < 0 ||
            product.PurchasePrice < 0)
        {
            throw new InvalidOperationException(
                $"Server Product '{product.Id}' contains " +
                $"a negative price.");
        }

        if (product.VatRate < 0 ||
            product.VatRate > 100)
        {
            throw new InvalidOperationException(
                $"Server Product '{product.Id}' contains " +
                $"an invalid VAT rate.");
        }

        if (product.MinStockLevel < 0 ||
            product.MaxStockLevel < 0 ||
            product.MinStockLevel >
            product.MaxStockLevel)
        {
            throw new InvalidOperationException(
                $"Server Product '{product.Id}' contains " +
                $"invalid stock limits.");
        }
    }

    private static Guid? NormalizeGuid(
        Guid? value)
    {
        return value.HasValue &&
               value.Value != Guid.Empty
            ? value.Value
            : null;
    }

    private static string FirstNotEmpty(
        string? first,
        string? second,
        string fallback)
    {
        if (!string.IsNullOrWhiteSpace(first))
            return first.Trim();

        if (!string.IsNullOrWhiteSpace(second))
            return second.Trim();

        return fallback;
    }

    private static string? FirstNotEmptyOrNull(
        string? first,
        string? second)
    {
        if (!string.IsNullOrWhiteSpace(first))
            return first.Trim();

        if (!string.IsNullOrWhiteSpace(second))
            return second.Trim();

        return null;
    }

    private static string? NullIfWhiteSpace(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string Truncate(
        string value,
        int maximumLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}