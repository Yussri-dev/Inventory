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
    private HashSet<Guid> _deletedServerIds = new();
    private const string ProductEntityName = "Product";
    private const string FullSyncMode = "Full";


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

            if (_tenantContext.GetRequiredTenantId() != tenantId)
                throw new InvalidOperationException("Le magasin a changé pendant le téléchargement.");

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

                ValidateLocalProductIdentity(localProducts);

                var localByServerId =
                    localProducts
                        .Where(x =>
                            x.ServerId.HasValue &&
                            x.ServerId.Value != Guid.Empty)
                        .ToDictionary(
                            x => x.ServerId!.Value);

                var localByCatalogId =
                     localProducts
                         .Where(x =>
                             x.CatalogProductId.HasValue &&
                             x.CatalogProductId.Value != Guid.Empty &&
                             !x.IsDeletedLocally)
                         .ToDictionary(
                             x => x.CatalogProductId!.Value);

                foreach (var serverProduct in serverProducts)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    ValidateServerProduct(
                        serverProduct);
                    LocalProductCatalog? catalog = null;

                    if (serverProduct.CatalogProductId.HasValue &&
                        serverProduct.CatalogProductId.Value != Guid.Empty)
                    {
                        if (!catalogs.TryGetValue(
                                serverProduct.CatalogProductId.Value,
                                out catalog))
                        {
                            throw new InvalidOperationException(
                                $"Catalog product " +
                                $"'{serverProduct.CatalogProductId.Value}' " +
                                $"does not exist in SQLite. " +
                                $"Synchronize ProductCatalog before Product.");
                        }
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
                    else if (
                serverProduct.CatalogProductId.HasValue &&
                serverProduct.CatalogProductId.Value != Guid.Empty &&
                localByCatalogId.TryGetValue(
                    serverProduct.CatalogProductId.Value, out var byCatalog))
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

                    string? categoryName = null;

                    if (catalog != null &&
                        catalog.CategoryId != Guid.Empty)
                    {
                        categories.TryGetValue(
                            catalog.CategoryId,
                            out categoryName);
                    }

                    ApplyServerProduct(
                        localProduct,
                        serverProduct,
                        catalog,
                        categoryName,
                        tenantId,
                        now);

                    localByServerId[serverProduct.Id] =
                        localProduct;

                    if (serverProduct.CatalogProductId.HasValue && serverProduct.CatalogProductId.Value != Guid.Empty)
                    {
                        localByCatalogId[
                            serverProduct.CatalogProductId.Value] =
                            localProduct;
                    }
                }

                /*
                 * ComponentProductId contient un ServerId.
                 * Après avoir inséré tous les produits, nous pouvons
                 * résoudre le LocalProduct correspondant.
                 */
                ResolvePackLocalProductIds(
                    localProducts);

                var protectedStockIds = await Inventory.LocalDB.Services.PendingStockProtection.GetProductIdsAsync(_db, tenantId, cancellationToken);
                // Explicit tombstones only; never infer deletion from an incomplete/failed response.
                foreach (var row in localProducts.Where(x => x.ServerId.HasValue && _deletedServerIds.Contains(x.ServerId.Value)))
                {
                    if (pendingLocalIds.Contains(row.Id) || protectedStockIds.Contains(row.Id)) continue;
                    row.IsDeletedLocally = true;
                    row.IsActive = false;
                    row.DeletedAtUtc = now;
                    row.LastSyncedAtUtc = now;
                }

                await MarkSyncSucceededAsync(
                    tenantId,
                    now,
                    cancellationToken);

                await _db.SaveChangesAsync(cancellationToken);

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

            LocalProductCatalog? catalog = null;

            if (serverProduct.CatalogProductId.HasValue &&
                serverProduct.CatalogProductId.Value != Guid.Empty)
            {
                catalog =
                    await _db.ProductCatalogs
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x =>
                                x.Id ==
                                    serverProduct.CatalogProductId.Value &&
                                !x.IsDeleted,
                            cancellationToken);

                if (catalog == null)
                {
                    throw new InvalidOperationException(
                        $"Catalog product " +
                        $"'{serverProduct.CatalogProductId.Value}' " +
                        $"was not found locally.");
                }
            }

            string? categoryName = null;

            if (catalog != null &&
                catalog.CategoryId != Guid.Empty)
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
             * 1. Best reconciliation case:
             * the server response comes from an offline-created
             * local product that was just uploaded.
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

            /*
             * 2. Normal server identity reconciliation.
             */
            localProduct ??=
                await _db.Products
                    .FirstOrDefaultAsync(
                        x =>
                            x.TenantId == tenantId &&
                            x.ServerId == serverProduct.Id,
                        cancellationToken);

            /*
             * 3. Catalog reconciliation is only valid
             * when a real CatalogProductId exists.
             *
             * Never match custom products using null == null.
             */
            if (localProduct == null &&
                serverProduct.CatalogProductId.HasValue &&
                serverProduct.CatalogProductId.Value != Guid.Empty)
            {
                localProduct =
                    await _db.Products
                        .FirstOrDefaultAsync(
                            x =>
                                x.TenantId == tenantId &&
                                x.CatalogProductId ==
                                    serverProduct.CatalogProductId.Value &&
                                !x.IsDeletedLocally,
                            cancellationToken);
            }

            /*
             * A normal server pull must not overwrite
             * pending local changes.
             *
             * When originatingLocalId exists, this response
             * is the confirmation of the local upload itself.
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

            /*
             * No matching local product exists.
             */
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

            /*
             * Prevent accidental relinking of one local product
             * to another server product.
             */
            if (localProduct.ServerId.HasValue &&
                localProduct.ServerId.Value != Guid.Empty &&
                localProduct.ServerId.Value != serverProduct.Id)
            {
                throw new InvalidOperationException(
                    $"Local Product '{localProduct.Id}' is linked to " +
                    $"another server Product.");
            }

            /*
             * Works for both:
             *
             * Catalog product:
             * CatalogProductId != null
             * catalog != null
             *
             * Custom product:
             * CatalogProductId == null
             * catalog == null
             */
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
             * The server response confirms that the
             * originating offline operation was accepted.
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

        //await CompleteQueueItemsAsync(
        //    tenantId,
        //    localProduct.Id,
        //    now,
        //    cancellationToken);

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
        var snapshot =
            await _productApi.DownloadSnapshot(
                cancellationToken);

        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.Items == null ||
            snapshot.DeletedIds == null)
        {
            throw new InvalidOperationException(
                "Incomplete synchronization snapshot.");
        }

        _deletedServerIds =
            snapshot.DeletedIds.ToHashSet();

        // =========================================
        // TEMP DEBUG PRICES
        // =========================================

        var poms = snapshot.Items
            .FirstOrDefault(x =>
                x.Barcode == "5449000318411");

        if (poms != null)
        {
            _logger.LogWarning(
                "SYNC DEBUG POMS -> " +
                "Id={Id}, Purchase={Purchase}, " +
                "Sale={Sale}, Sale2={Sale2}, Sale3={Sale3}",
                poms.Id,
                poms.PurchasePrice,
                poms.SalePrice,
                poms.SalePrice2,
                poms.SalePrice3);
        }
        else
        {
            _logger.LogWarning(
                "SYNC DEBUG POMS -> NOT FOUND IN SERVER SNAPSHOT");
        }

        return snapshot.Items;
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
    LocalProductCatalog? catalog,
    string? categoryName,
    Guid tenantId,
    DateTime now)
    {
        // ============================================================
        // IDENTITY
        // ============================================================

        localProduct.TenantId =
            tenantId;

        localProduct.ServerId =
            serverProduct.Id;

        localProduct.CatalogProductId =
            NormalizeGuid(
                serverProduct.CatalogProductId);

        var isCatalogProduct =
            catalog != null;

        // ============================================================
        // NAME
        // ============================================================

        localProduct.Name =
            isCatalogProduct
                ? FirstNotEmpty(
                    catalog!.Name,
                    serverProduct.Name,
                    $"Product {serverProduct.Id}")
                : FirstNotEmpty(
                    serverProduct.Name,
                    null,
                    $"Product {serverProduct.Id}");

        // ============================================================
        // SKU
        // ============================================================

        localProduct.Sku =
            isCatalogProduct
                ? NullIfWhiteSpace(
                    catalog!.InternalCode)
                : NullIfWhiteSpace(
                    serverProduct.Sku);

        // ============================================================
        // BARCODE
        // ============================================================

        /*
         * Very important after the barcode repair:
         *
         * linked Product:
         * ProductCatalog is canonical.
         *
         * custom Product:
         * server Product is canonical.
         */
        localProduct.Barcode = isCatalogProduct
         ? NullIfWhiteSpace(catalog!.Barcode)
         : NullIfWhiteSpace(serverProduct.Barcode);

        // ============================================================
        // DESCRIPTION
        // ============================================================

        localProduct.Description =
            isCatalogProduct
                ? FirstNotEmptyOrNull(
                    catalog!.Description,
                    serverProduct.Description)
                : NullIfWhiteSpace(
                    serverProduct.Description);

        // ============================================================
        // BRAND
        // ============================================================

        localProduct.Brand =
            isCatalogProduct
                ? FirstNotEmptyOrNull(
                    catalog!.Brand,
                    serverProduct.Brand)
                : NullIfWhiteSpace(
                    serverProduct.Brand);

        // ============================================================
        // CATEGORY
        // ============================================================

        localProduct.Category =
            isCatalogProduct
                ? NullIfWhiteSpace(
                    categoryName)
                : NullIfWhiteSpace(
                    serverProduct.Category);

        // ============================================================
        // UNIT
        // ============================================================

        if (isCatalogProduct)
        {
            localProduct.Unit =
                !string.IsNullOrWhiteSpace(
                    catalog!.UnitOfMeasure)
                    ? catalog.UnitOfMeasure.Trim()
                    : !string.IsNullOrWhiteSpace(
                        serverProduct.Unit)
                        ? serverProduct.Unit.Trim()
                        : "pcs";
        }
        else
        {
            localProduct.Unit =
                !string.IsNullOrWhiteSpace(
                    serverProduct.Unit)
                    ? serverProduct.Unit.Trim()
                    : "pcs";
        }

        // ============================================================
        // TENANT PRICING
        // ============================================================

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

        // ============================================================
        // STOCK CONFIGURATION
        // ============================================================

        localProduct.MinStockLevel =
            serverProduct.MinStockLevel;

        localProduct.MaxStockLevel =
            serverProduct.MaxStockLevel;

        // ============================================================
        // STATUS
        // ============================================================

        localProduct.Status =
            serverProduct.Status;

        localProduct.IsActive =
            serverProduct.Status ==
            ProductStatus.Active;

        localProduct.IsTracked =
            serverProduct.IsTracked;

        // ============================================================
        // PACK
        // ============================================================

        localProduct.IsPack =
            serverProduct.IsPack ||
            (catalog?.IsPack ?? false);

        localProduct.UnitProductServerId =
            NormalizeGuid(
                serverProduct.ComponentProductId);

        localProduct.UnitsPerPack =
            serverProduct.PackSize > 0m
                ? serverProduct.PackSize
                : 1m;

        // ============================================================
        // SYNC STATE
        // ============================================================

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
        var productList =
            products.ToList();

        var duplicateServerIds =
            productList
                .Where(x =>
                    x.ServerId.HasValue &&
                    x.ServerId.Value != Guid.Empty)
                .GroupBy(x => x.ServerId!.Value)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

        if (duplicateServerIds.Count > 0)
        {
            throw new InvalidOperationException(
                "Cannot resolve pack products because duplicate " +
                "ServerIds exist in SQLite: " +
                string.Join(", ", duplicateServerIds));
        }

        var productsByServerId =
            productList
                .Where(x =>
                    x.ServerId.HasValue &&
                    x.ServerId.Value != Guid.Empty)
                .ToDictionary(
                    x => x.ServerId!.Value);

        foreach (var product in productList)
        {
            if (!product.UnitProductServerId.HasValue ||
                product.UnitProductServerId.Value == Guid.Empty)
            {
                product.UnitProductLocalId = null;
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
            product.UnitProductLocalId = null;
            return;
        }

        var matches =
            await _db.Products
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.ServerId ==
                        product.UnitProductServerId.Value)
                .Select(x => x.Id)
                .Take(2)
                .ToListAsync(cancellationToken);

        if (matches.Count > 1)
        {
            throw new InvalidOperationException(
                $"Multiple local Products reference ServerId " +
                $"'{product.UnitProductServerId.Value}'.");
        }

        product.UnitProductLocalId =
            matches.Count == 1
                ? matches[0]
                : null;
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
        ArgumentNullException.ThrowIfNull(product);

        if (product.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The server returned a Product without an Id.");
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            throw new InvalidOperationException(
                $"Server Product '{product.Id}' does not contain a name.");
        }

        if (product.SalePrice < 0 ||
            product.SalePrice2 < 0 ||
            product.SalePrice3 < 0 ||
            product.PurchasePrice < 0)
        {
            throw new InvalidOperationException(
                $"Server Product '{product.Id}' contains a negative price.");
        }

        if (product.VatRate < 0 ||
            product.VatRate > 100)
        {
            throw new InvalidOperationException(
                $"Server Product '{product.Id}' contains an invalid VAT rate.");
        }

        if (product.MinStockLevel < 0 ||
            product.MaxStockLevel < 0 ||
            product.MinStockLevel >
            product.MaxStockLevel)
        {
            throw new InvalidOperationException(
                $"Server Product '{product.Id}' contains invalid stock limits.");
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

    public async Task ForceFullSyncAsync(
     CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        try
        {
            // ============================================================
            // DOWNLOAD SERVER SNAPSHOT
            // ============================================================

            var serverProducts =
                await DownloadAllServerProductsAsync(
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (_tenantContext.GetRequiredTenantId() != tenantId)
            {
                throw new InvalidOperationException(
                    "Le magasin a changé pendant le téléchargement.");
            }

            // ============================================================
            // SQLITE TRANSACTION
            // ============================================================

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var now =
                    DateTime.UtcNow;

                // ========================================================
                // LOCAL CATALOGS
                // ========================================================

                var catalogs =
                    await _db.ProductCatalogs
                        .AsNoTracking()
                        .Where(x =>
                            !x.IsDeleted)
                        .ToDictionaryAsync(
                            x => x.Id,
                            cancellationToken);

                // ========================================================
                // LOCAL CATEGORIES
                // ========================================================

                var categories =
                    await _db.ProductCategories
                        .AsNoTracking()
                        .Where(x =>
                            !x.IsDeleted)
                        .ToDictionaryAsync(
                            x => x.Id,
                            x => x.Name,
                            cancellationToken);

                // ========================================================
                // LOCAL PRODUCTS
                // ========================================================

                var localProducts =
                    await _db.Products
                        .Where(x =>
                            x.TenantId == tenantId)
                        .ToListAsync(
                            cancellationToken);

                ValidateLocalProductIdentity(localProducts);

                // ========================================================
                // INDEX BY SERVER ID
                // ========================================================

                var localByServerId =
                    localProducts
                        .Where(x =>
                            x.ServerId.HasValue &&
                            x.ServerId.Value != Guid.Empty)
                        .ToDictionary(
                            x => x.ServerId!.Value);

                // ========================================================
                // INDEX BY CATALOG ID
                // ========================================================

                /*
                 * Catalog reconciliation is only allowed when
                 * CatalogProductId has a real value.
                 *
                 * Never reconcile custom products using null == null.
                 */

                var localByCatalogId =
                    localProducts
                        .Where(x =>
                            x.CatalogProductId.HasValue &&
                            x.CatalogProductId.Value != Guid.Empty &&
                            !x.IsDeletedLocally)
                        .ToDictionary(
                            x => x.CatalogProductId!.Value);

                // ========================================================
                // TRACK PRODUCTS ACTUALLY FORCE-SYNCHRONIZED
                // ========================================================

                /*
                 * Important:
                 *
                 * We must NOT mark every Product queue operation as Done.
                 *
                 * A Product created only offline may not exist in the
                 * server snapshot yet.
                 *
                 * Only queue entries associated with Products actually
                 * reconciled with the server snapshot are discarded.
                 */

                var forceSyncedLocalIds =
                    new HashSet<Guid>();

                // ========================================================
                // APPLY SERVER PRODUCTS
                // ========================================================

                foreach (var serverProduct in serverProducts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ValidateServerProduct(
                        serverProduct);

                    LocalProductCatalog? catalog = null;

                    // ====================================================
                    // CATALOG
                    // ====================================================

                    if (serverProduct.CatalogProductId.HasValue &&
                        serverProduct.CatalogProductId.Value != Guid.Empty)
                    {
                        if (!catalogs.TryGetValue(
                                serverProduct.CatalogProductId.Value,
                                out catalog))
                        {
                            throw new InvalidOperationException(
                                $"Catalog product " +
                                $"'{serverProduct.CatalogProductId.Value}' " +
                                $"does not exist in SQLite. " +
                                $"Synchronize ProductCatalog before Product.");
                        }
                    }

                    // ====================================================
                    // FIND LOCAL PRODUCT
                    // ====================================================

                    LocalProduct? localProduct = null;

                    /*
                     * Priority 1:
                     * ServerId
                     */

                    if (localByServerId.TryGetValue(
                            serverProduct.Id,
                            out var byServer))
                    {
                        localProduct =
                            byServer;
                    }

                    /*
                     * Priority 2:
                     * CatalogProductId.
                     */

                    else if (
                        serverProduct.CatalogProductId.HasValue &&
                        serverProduct.CatalogProductId.Value != Guid.Empty &&
                        localByCatalogId.TryGetValue(
                            serverProduct.CatalogProductId.Value,
                            out var byCatalog))
                    {
                        localProduct =
                            byCatalog;
                    }

                    // ====================================================
                    // CREATE LOCAL PRODUCT IF NEEDED
                    // ====================================================

                    if (localProduct == null)
                    {
                        localProduct =
                            new LocalProduct
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                CreatedAtUtc = now
                            };

                        _db.Products.Add(
                            localProduct);

                        localProducts.Add(
                            localProduct);
                    }

                    // ====================================================
                    // SAFETY
                    // ====================================================

                    /*
                     * Never silently relink an existing local Product
                     * to a different server Product.
                     */

                    if (localProduct.ServerId.HasValue &&
                        localProduct.ServerId.Value != Guid.Empty &&
                        localProduct.ServerId.Value != serverProduct.Id)
                    {
                        throw new InvalidOperationException(
                            $"Local Product '{localProduct.Id}' is already " +
                            $"linked to server Product " +
                            $"'{localProduct.ServerId.Value}', but the server " +
                            $"returned Product '{serverProduct.Id}'.");
                    }

                    // ====================================================
                    // CATEGORY
                    // ====================================================

                    string? categoryName = null;

                    if (catalog != null &&
                        catalog.CategoryId != Guid.Empty)
                    {
                        categories.TryGetValue(
                            catalog.CategoryId,
                            out categoryName);
                    }
                    // ====================================================
                    // FORCE SERVER DATA OVER LOCAL DATA
                    // ====================================================

                    ApplyServerProduct(
                        localProduct,
                        serverProduct,
                        catalog,
                        categoryName,
                        tenantId,
                        now);

                    if (serverProduct.Barcode == "5449000318411")
                    {
                        _logger.LogWarning(
                            "SYNC DEBUG POMS AFTER APPLY -> " +
                            "LocalId={LocalId}, ServerId={ServerId}, " +
                            "Purchase={Purchase}, Sale={Sale}, " +
                            "Sale2={Sale2}, Sale3={Sale3}",
                            localProduct.Id,
                            localProduct.ServerId,
                            localProduct.PurchasePrice,
                            localProduct.SalePrice,
                            localProduct.SalePrice2,
                            localProduct.SalePrice3);
                    }

                    // ====================================================
                    // REGISTER AS FORCE-SYNCHRONIZED
                    // ====================================================

                    forceSyncedLocalIds.Add(
                        localProduct.Id);

                    // ====================================================
                    // UPDATE LOOKUP MAPS
                    // ====================================================

                    localByServerId[
                        serverProduct.Id] =
                        localProduct;

                    if (serverProduct.CatalogProductId.HasValue &&
                        serverProduct.CatalogProductId.Value != Guid.Empty)
                    {
                        localByCatalogId[
                            serverProduct.CatalogProductId.Value] =
                            localProduct;
                    }
                }

                // ========================================================
                // COMPLETE OBSOLETE PRODUCT QUEUE ITEMS
                // ========================================================

                /*
                 * One query only.
                 *
                 * Only queue operations for products actually present in
                 * the accepted server snapshot are marked Done.
                 *
                 * Purely local/offline Products remain pending.
                 */

                if (forceSyncedLocalIds.Count > 0)
                {
                    var productQueueItems =
                        await _db.SyncQueueItems
                            .Where(x =>
                                x.TenantId == tenantId &&
                                x.EntityName == ProductEntityName &&
                                x.Status != SyncQueueStatus.Done &&
                                forceSyncedLocalIds.Contains(
                                    x.LocalEntityId))
                            .ToListAsync(
                                cancellationToken);

                    foreach (var queueItem in productQueueItems)
                    {
                        queueItem.Status =
                            SyncQueueStatus.Done;

                        queueItem.ProcessedAtUtc =
                            now;

                        queueItem.ErrorMessage =
                            null;
                    }
                }

                // ========================================================
                // PACK LOCAL IDS
                // ========================================================

                ResolvePackLocalProductIds(
                    localProducts);

                // ========================================================
                // PROTECT PENDING STOCK OPERATIONS
                // ========================================================

                var protectedStockIds =
                    await Inventory.LocalDB.Services
                        .PendingStockProtection
                        .GetProductIdsAsync(
                            _db,
                            tenantId,
                            cancellationToken);

                // ========================================================
                // SERVER DELETIONS
                // ========================================================

                /*
                 * Explicit tombstones only.
                 *
                 * Do not infer deletion because a record was absent from
                 * an incomplete response.
                 *
                 * Also preserve Products referenced by pending Stock
                 * operations.
                 */

                foreach (var localProduct in
                         localProducts.Where(x =>
                             x.ServerId.HasValue &&
                             x.ServerId.Value != Guid.Empty &&
                             _deletedServerIds.Contains(
                                 x.ServerId.Value)))
                {
                    if (protectedStockIds.Contains(
                            localProduct.Id))
                    {
                        _logger.LogWarning(
                            "Skipping forced deletion of Product {LocalId} " +
                            "because pending Stock operations reference it.",
                            localProduct.Id);

                        continue;
                    }

                    localProduct.IsDeletedLocally =
                        true;

                    localProduct.IsActive =
                        false;

                    localProduct.DeletedAtUtc =
                        now;

                    localProduct.ModifiedAtUtc =
                        now;

                    localProduct.LastSyncedAtUtc =
                        now;

                    localProduct.SyncStatus =
                        SyncQueueStatus.Done;

                    /*
                     * No CompleteQueueItemsAsync() here.
                     *
                     * Relevant Product queue entries were already handled
                     * in one batch above.
                     */
                }

                // ========================================================
                // SYNC STATE
                // ========================================================

                await MarkSyncSucceededAsync(
                    tenantId,
                    now,
                    cancellationToken);

                // ========================================================
                // SAVE
                // ========================================================

                await _db.SaveChangesAsync(cancellationToken);

                var savedPoms = await _db.Products
    .AsNoTracking()
    .FirstOrDefaultAsync(
        x =>
            x.TenantId == tenantId &&
            x.Barcode == "5449000318411",
        cancellationToken);

                if (savedPoms != null)
                {
                    _logger.LogWarning(
                        "SYNC DEBUG POMS AFTER SAVE -> " +
                        "LocalId={LocalId}, " +
                        "Purchase={Purchase}, Sale={Sale}, " +
                        "Sale2={Sale2}, Sale3={Sale3}",
                        savedPoms.Id,
                        savedPoms.PurchasePrice,
                        savedPoms.SalePrice,
                        savedPoms.SalePrice2,
                        savedPoms.SalePrice3);
                }

                // ========================================================
                // COMMIT
                // ========================================================

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "FORCED Product full synchronization completed for " +
                    "tenant {TenantId}. {Count} server products processed.",
                    tenantId,
                    serverProducts.Count);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);

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
                "Forced Product full synchronization failed " +
                "for tenant {TenantId}.",
                tenantId);

            throw;
        }
    }

    private static void ValidateLocalProductIdentity(
      IReadOnlyCollection<LocalProduct> products)
    {
        // ============================================================
        // SERVER ID
        // ============================================================

        var duplicateServerIds =
            products
                .Where(x =>
                    x.ServerId.HasValue &&
                    x.ServerId.Value != Guid.Empty)
                .GroupBy(x => x.ServerId!.Value)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

        if (duplicateServerIds.Count > 0)
        {
            throw new InvalidOperationException(
                "Duplicate Product ServerId detected in SQLite: " +
                string.Join(", ", duplicateServerIds));
        }

        // ============================================================
        // CATALOG PRODUCT ID
        // ============================================================

        var duplicateCatalogIds =
            products
                .Where(x =>
                    !x.IsDeletedLocally &&
                    x.CatalogProductId.HasValue &&
                    x.CatalogProductId.Value != Guid.Empty)
                .GroupBy(x => x.CatalogProductId!.Value)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

        if (duplicateCatalogIds.Count > 0)
        {
            throw new InvalidOperationException(
                "Duplicate active Product CatalogProductId detected in SQLite: " +
                string.Join(", ", duplicateCatalogIds));
        }

        // ============================================================
        // BARCODE
        // ============================================================

        var duplicateBarcodes =
            products
                .Where(x =>
                    !x.IsDeletedLocally &&
                    x.IsActive &&
                    !string.IsNullOrWhiteSpace(x.Barcode))
                .GroupBy(
                    x => x.Barcode!.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => new
                {
                    Barcode = x.Key,

                    ProductIds = x
                        .Select(p => p.Id)
                        .ToList()
                })
                .ToList();

        if (duplicateBarcodes.Count > 0)
        {
            var details =
                string.Join(
                    "; ",
                    duplicateBarcodes.Select(x =>
                        $"{x.Barcode} => " +
                        string.Join(", ", x.ProductIds)));

            throw new InvalidOperationException(
                "Duplicate active Product barcode detected in SQLite: " +
                details);
        }
    }
}