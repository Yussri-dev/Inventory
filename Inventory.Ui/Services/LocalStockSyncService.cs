using Inventory.Dto.Queries;
using Inventory.Dto.Stock.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services;

public sealed class LocalStockSyncService
    : ILocalStockSyncService
{
    private const string StockEntityName = "Stock";
    private const string FullSyncMode = "Full";

    private const int PageSize = 100;
    private const int MaximumPages = 10_000;

    private readonly PosLocalDbContext _db;
    private readonly IStockApi _stockApi;
    private readonly ILocalStockMovementUploadService
        _stockMovementUploadService;
    private readonly ILocalTenantContext _tenantContext;
    private readonly ILogger<LocalStockSyncService> _logger;

    public LocalStockSyncService(
        PosLocalDbContext db,
        IStockApi stockApi,
        ILocalStockMovementUploadService stockMovementUploadService,
        ILocalTenantContext tenantContext,
        ILogger<LocalStockSyncService> logger)
    {
        _db = db;
        _stockApi = stockApi;
        _stockMovementUploadService =
            stockMovementUploadService;
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
             * Push manual local stock adjustments before pulling
             * authoritative stock values from the server.
             */
            var adjustmentUpload =
                await _stockMovementUploadService
                    .SyncPendingAsync(
                        cancellationToken);

            if (adjustmentUpload.Failed > 0)
            {
                _logger.LogWarning(
                    "Stock adjustment upload finished with {Failed} " +
                    "failure(s). Local stocks with pending adjustment " +
                    "queues will be protected from server overwrite.",
                    adjustmentUpload.Failed);
            }

            /*
             * Download stocks before opening the SQLite transaction.
             */
            var serverStocks =
                await DownloadAllStocksAsync(
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
                 * Les produits doivent déjà être synchronisés.
                 *
                 * StockResult.ProductId correspond au Product.Id
                 * de la base serveur.
                 */
                var localProducts =
                    await _db.Products
                        .Where(product =>
                            product.TenantId == tenantId &&
                            !product.IsDeletedLocally)
                        .ToListAsync(cancellationToken);

                var productsByServerId =
                    localProducts
                        .Where(product =>
                            product.ServerId.HasValue &&
                            product.ServerId.Value != Guid.Empty)
                        .GroupBy(product =>
                            product.ServerId!.Value)
                        .ToDictionary(
                            group => group.Key,
                            group => group.First());

                var localStocks =
                    await _db.Stocks
                        .Where(stock =>
                            stock.TenantId == tenantId)
                        .ToListAsync(cancellationToken);

                var protectedProductLocalIds =
                    await GetProtectedProductLocalIdsAsync(
                        tenantId,
                        cancellationToken);

                var stocksByServerId =
                    localStocks
                        .Where(stock =>
                            stock.ServerId.HasValue &&
                            stock.ServerId.Value != Guid.Empty)
                        .GroupBy(stock =>
                            stock.ServerId!.Value)
                        .ToDictionary(
                            group => group.Key,
                            group => group.First());

                var stocksByProductServerId =
                    localStocks
                        .Where(stock =>
                            stock.ProductServerId.HasValue &&
                            stock.ProductServerId.Value != Guid.Empty)
                        .GroupBy(stock =>
                            stock.ProductServerId!.Value)
                        .ToDictionary(
                            group => group.Key,
                            group => group.First());

                foreach (var serverStock in serverStocks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ValidateServerStock(serverStock);

                    if (!productsByServerId.TryGetValue(
                            serverStock.ProductId,
                            out var localProduct))
                    {
                        throw new InvalidOperationException(
                            $"The local Product for server Product " +
                            $"'{serverStock.ProductId}' was not found. " +
                            $"Synchronize Products before Stocks.");
                    }

                    LocalStock? localStock = null;

                    if (stocksByServerId.TryGetValue(
                            serverStock.Id,
                            out var stockByServerId))
                    {
                        localStock = stockByServerId;
                    }
                    else if (stocksByProductServerId.TryGetValue(
                                 serverStock.ProductId,
                                 out var stockByProduct))
                    {
                        localStock = stockByProduct;
                    }

                    if (localStock == null)
                    {
                        localStock = new LocalStock
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId
                        };

                        _db.Stocks.Add(localStock);
                        localStocks.Add(localStock);
                    }

                    if (protectedProductLocalIds.Contains(
                            localProduct.Id))
                    {
                        /*
                         * A pending/conflicted local adjustment still
                         * exists for this product. Keep the local
                         * quantity and only reconcile identities and
                         * descriptive metadata.
                         */
                        ApplyServerIdentity(
                            localStock,
                            serverStock,
                            localProduct,
                            tenantId);

                        localProduct.LocalStockQuantity =
                            localStock.Quantity;
                    }
                    else
                    {
                        ApplyServerStock(
                            localStock,
                            serverStock,
                            localProduct,
                            tenantId);

                        localProduct.LocalStockQuantity =
                            serverStock.Quantity;
                    }

                    stocksByServerId[serverStock.Id] =
                        localStock;

                    stocksByProductServerId[
                        serverStock.ProductId] =
                        localStock;
                }

                /*
                 * Créer un stock local à zéro pour les produits
                 * suivis qui n'ont pas encore de ligne Stock serveur.
                 */
                foreach (var localProduct in localProducts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!localProduct.IsTracked)
                        continue;

                    var hasLocalStock =
                        localStocks.Any(stock =>
                            stock.ProductLocalId ==
                            localProduct.Id);

                    if (hasLocalStock)
                        continue;

                    var zeroStock = new LocalStock
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ServerId = null,

                        ProductLocalId =
                            localProduct.Id,

                        ProductServerId =
                            localProduct.ServerId,

                        ProductName =
                            localProduct.Name,

                        ProductBarcode =
                            localProduct.Barcode,

                        Quantity = 0,
                        ReservedQuantity = 0,
                        LastUpdatedUtc = now,
                        LastSyncedAtUtc = now
                    };

                    _db.Stocks.Add(zeroStock);
                    localStocks.Add(zeroStock);

                    localProduct.LocalStockQuantity = 0;
                }

                await MarkSyncSucceededAsync(
                    tenantId,
                    now,
                    cancellationToken);

                await _db.SaveChangesAsync(
                    cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    "Stock synchronization completed for tenant " +
                    "{TenantId}. {Count} stocks were processed.",
                    tenantId,
                    serverStocks.Count);
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
                "Stock synchronization failed for tenant {TenantId}.",
                tenantId);

            throw;
        }
    }

    public async Task UpsertFromServerAsync(
        StockResult serverStock,
        CancellationToken cancellationToken = default)
    {
        ValidateServerStock(serverStock);

        var tenantId =
            _tenantContext.GetRequiredTenantId();

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var localProduct =
                await _db.Products
                    .FirstOrDefaultAsync(
                        product =>
                            product.TenantId == tenantId &&
                            product.ServerId ==
                                serverStock.ProductId &&
                            !product.IsDeletedLocally,
                        cancellationToken);

            if (localProduct == null)
            {
                throw new InvalidOperationException(
                    $"The local Product for server Product " +
                    $"'{serverStock.ProductId}' was not found.");
            }

            var localStock =
                await _db.Stocks
                    .FirstOrDefaultAsync(
                        stock =>
                            stock.TenantId == tenantId &&
                            stock.ServerId == serverStock.Id,
                        cancellationToken);

            localStock ??=
                await _db.Stocks
                    .FirstOrDefaultAsync(
                        stock =>
                            stock.TenantId == tenantId &&
                            stock.ProductServerId ==
                                serverStock.ProductId,
                        cancellationToken);

            if (localStock == null)
            {
                localStock = new LocalStock
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId
                };

                _db.Stocks.Add(localStock);
            }

            var protectedProductLocalIds =
                await GetProtectedProductLocalIdsAsync(
                    tenantId,
                    cancellationToken);

            if (protectedProductLocalIds.Contains(
                    localProduct.Id))
            {
                ApplyServerIdentity(
                    localStock,
                    serverStock,
                    localProduct,
                    tenantId);

                localProduct.LocalStockQuantity =
                    localStock.Quantity;
            }
            else
            {
                ApplyServerStock(
                    localStock,
                    serverStock,
                    localProduct,
                    tenantId);

                localProduct.LocalStockQuantity =
                    serverStock.Quantity;
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

    public async Task<bool> HasInitialSyncCompletedAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.GetRequiredTenantId();

        return await _db.SyncTableStates
            .AsNoTracking()
            .AnyAsync(
                state =>
                    state.TenantId == tenantId &&
                    state.EntityName == StockEntityName &&
                    state.InitialSyncCompleted,
                cancellationToken);
    }

    private async Task<List<StockResult>>
        DownloadAllStocksAsync(
            CancellationToken cancellationToken)
    {
        var stocks =
            new List<StockResult>();

        for (var page = 1;
             page <= MaximumPages;
             page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response =
                await _stockApi.Search(
                    new StockQuery
                    {
                        Page = page,
                        PageSize = PageSize
                    },
                    cancellationToken);

            var pageItems =
                response.Items?.ToList()
                ?? new List<StockResult>();

            if (pageItems.Count == 0)
                break;

            stocks.AddRange(pageItems);

            if (response.TotalCount > 0 &&
                stocks.Count >= response.TotalCount)
            {
                break;
            }

            if (pageItems.Count < PageSize)
                break;

            if (page == MaximumPages)
            {
                throw new InvalidOperationException(
                    "Stock synchronization exceeded the maximum " +
                    "number of pages.");
            }
        }

        return stocks
            .Where(stock =>
                stock.Id != Guid.Empty)
            .GroupBy(stock =>
                stock.Id)
            .Select(group =>
                group.Last())
            .ToList();
    }

    private static void ApplyServerStock(
        LocalStock localStock,
        StockResult serverStock,
        LocalProduct localProduct,
        Guid tenantId)
    {
        localStock.TenantId =
            tenantId;

        localStock.ServerId =
            serverStock.Id;

        localStock.ProductLocalId =
            localProduct.Id;

        localStock.ProductServerId =
            serverStock.ProductId;

        localStock.ProductName =
            string.IsNullOrWhiteSpace(serverStock.Name)
                ? localProduct.Name
                : serverStock.Name.Trim();

        localStock.ProductBarcode =
            localProduct.Barcode;

        localStock.Quantity =
            serverStock.Quantity;

        localStock.ReservedQuantity =
            serverStock.ReservedQuantity;

        var now =
            DateTime.UtcNow;

        localStock.LastUpdatedUtc =
            now;

        localStock.LastSyncedAtUtc =
            now;
    }

    private static void ApplyServerIdentity(
        LocalStock localStock,
        StockResult serverStock,
        LocalProduct localProduct,
        Guid tenantId)
    {
        localStock.TenantId =
            tenantId;

        localStock.ServerId =
            serverStock.Id;

        localStock.ProductLocalId =
            localProduct.Id;

        localStock.ProductServerId =
            serverStock.ProductId;

        localStock.ProductName =
            string.IsNullOrWhiteSpace(
                serverStock.Name)
                ? localProduct.Name
                : serverStock.Name.Trim();

        localStock.ProductBarcode =
            localProduct.Barcode;
    }

    private async Task<HashSet<Guid>>
        GetProtectedProductLocalIdsAsync(
            Guid tenantId,
            CancellationToken cancellationToken)
    {
        const string stockMovementEntityName =
            "StockMovement";

        var queueMovementIds =
            await _db.SyncQueueItems
                .AsNoTracking()
                .Where(item =>
                    item.TenantId == tenantId &&
                    item.EntityName ==
                        stockMovementEntityName &&
                    item.Status !=
                        SyncQueueStatus.Done)
                .Select(item =>
                    item.LocalEntityId)
                .ToListAsync(
                    cancellationToken);

        if (queueMovementIds.Count ==
            0)
        {
            return new HashSet<Guid>();
        }

        var productIds =
            await _db.StockMovements
                .AsNoTracking()
                .Where(movement =>
                    movement.TenantId == tenantId &&
                    queueMovementIds.Contains(
                        movement.Id))
                .Select(movement =>
                    movement.ProductLocalId)
                .Distinct()
                .ToListAsync(
                    cancellationToken);

        return productIds.ToHashSet();
    }

    private async Task MarkSyncSucceededAsync(
        Guid tenantId,
        DateTime synchronizedAtUtc,
        CancellationToken cancellationToken)
    {
        var state =
            await _db.SyncTableStates
                .FirstOrDefaultAsync(
                    item =>
                        item.TenantId == tenantId &&
                        item.EntityName == StockEntityName,
                    cancellationToken);

        if (state == null)
        {
            state = new SyncTableStateLocal
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EntityName = StockEntityName,
                Syncmode = FullSyncMode
            };

            _db.SyncTableStates.Add(state);
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
            _db.ChangeTracker.Clear();

            var state =
                await _db.SyncTableStates
                    .FirstOrDefaultAsync(
                        item =>
                            item.TenantId == tenantId &&
                            item.EntityName == StockEntityName,
                        CancellationToken.None);

            if (state == null)
            {
                state = new SyncTableStateLocal
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    EntityName = StockEntityName,
                    Syncmode = FullSyncMode,
                    InitialSyncCompleted = false
                };

                _db.SyncTableStates.Add(state);
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
                "Could not persist Stock synchronization failure " +
                "for tenant {TenantId}.",
                tenantId);
        }
    }

    private static void ValidateServerStock(
        StockResult stock)
    {
        ArgumentNullException.ThrowIfNull(stock);

        if (stock.Id == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The server returned a Stock without an Id.");
        }

        if (stock.ProductId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Server Stock '{stock.Id}' does not contain " +
                "a ProductId.");
        }

        if (stock.Quantity < 0)
        {
            throw new InvalidOperationException(
                $"Server Stock '{stock.Id}' contains " +
                "a negative quantity.");
        }

        if (stock.ReservedQuantity < 0)
        {
            throw new InvalidOperationException(
                $"Server Stock '{stock.Id}' contains " +
                "a negative reserved quantity.");
        }

        if (stock.ReservedQuantity > stock.Quantity)
        {
            throw new InvalidOperationException(
                $"Server Stock '{stock.Id}' contains a reserved " +
                "quantity greater than the total quantity.");
        }
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