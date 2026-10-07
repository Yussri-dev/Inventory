using Inventory.Dto.PackComponent.Results;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Queries;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.Ui.Interfaces;
using Inventory.Ui.Services.Sync;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Inventory.Ui.Services
{
    public sealed class LocalProductCatalogSyncService
     : ILocalProductCatalogSyncService
    {

        private readonly PosLocalDbContext _db;
        private readonly IProductCatalogApi _productCatalogApi;

        public LocalProductCatalogSyncService(
            PosLocalDbContext db,
            IProductCatalogApi productCatalogApi)
        {
            _db = db;
            _productCatalogApi = productCatalogApi;
        }

        public async Task FullSyncAsync(
    CancellationToken cancellationToken = default)
        {
            var totalDownloaded = 0;
            var syncStartedAtUtc = DateTime.UtcNow;

            var dbPath =
                _db.Database
                    .GetDbConnection()
                    .DataSource;

            Debug.WriteLine(
                $"Product catalog sync database: {dbPath}");

            try
            {
                var snapshot =
                    await _productCatalogApi
                        .DownloadSnapshot(
                            cancellationToken);

                if (snapshot?.Items == null ||
                    snapshot.DeletedIds == null)
                {
                    throw new InvalidOperationException(
                        "Incomplete catalog snapshot.");
                }

                await using var transaction =
                    await _db.Database
                        .BeginTransactionAsync(
                            cancellationToken);

                try
                {
                    await SynchronizePageAsync(
                        snapshot.Items,
                        syncStartedAtUtc,
                        cancellationToken);

                    totalDownloaded =
                        snapshot.Items.Count;

                    await MarkMissingCatalogsAsDeletedAsync(
                        snapshot.DeletedIds,
                        syncStartedAtUtc,
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

                _db.ChangeTracker.Clear();

                Debug.WriteLine(
                    $"Product catalog synchronization completed. " +
                    $"Downloaded: {totalDownloaded}.");
            }
            catch (OperationCanceledException)
            {
                _db.ChangeTracker.Clear();

                Debug.WriteLine(
                    $"Product catalog synchronization cancelled after " +
                    $"{totalDownloaded} records.");

                throw;
            }
            catch (Exception exception)
            {
                _db.ChangeTracker.Clear();

                Debug.WriteLine(
                    $"Product catalog synchronization failed after " +
                    $"{totalDownloaded} records. Error: {exception}");

                throw new InvalidOperationException(
                    $"Product catalog synchronization failed after " +
                    $"{totalDownloaded} records.",
                    exception);
            }
        }

        public async Task UpsertAsync(
     ProductCatalogResult catalog,
     CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                catalog);

            cancellationToken
                .ThrowIfCancellationRequested();

            var localCatalog =
                await _db.ProductCatalogs
                    .Include(item =>
                        item.PackComponents)
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id == catalog.Id,
                        cancellationToken);

            if (localCatalog == null)
            {
                localCatalog =
                    new LocalProductCatalog
                    {
                        Id =
                            catalog.Id
                    };

                await _db.ProductCatalogs.AddAsync(
                    localCatalog,
                    cancellationToken);
            }

            var now =
                DateTime.UtcNow;

            MapToLocal(
                catalog,
                localCatalog,
                now);

            SyncPackComponents(
                catalog,
                localCatalog);

            await _db.SaveChangesAsync(
                cancellationToken);
        }

        public async Task MarkDeletedAsync(
            Guid catalogId,
            CancellationToken cancellationToken = default)
        {
            if (catalogId == Guid.Empty)
            {
                throw new ArgumentException(
                    "The product catalog ID cannot be empty.",
                    nameof(catalogId));
            }

            var localCatalog = await _db.ProductCatalogs
                .FirstOrDefaultAsync(
                    x => x.Id == catalogId,
                    cancellationToken);

            if (localCatalog == null)
                return;

            localCatalog.IsDeleted = true;
            localCatalog.LastSyncedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
        }

        public Task<bool> HasLocalDataAsync(
            CancellationToken cancellationToken = default)
        {
            return _db.ProductCatalogs
                .AsNoTracking()
                .AnyAsync(
                    x => !x.IsDeleted,
                    cancellationToken);
        }

        private async Task SynchronizePageAsync(
      IReadOnlyCollection<ProductCatalogResult> serverCatalogs,
      DateTime syncStartedAtUtc,
      CancellationToken cancellationToken)
        {
            var serverIds =
                serverCatalogs
                    .Select(x => x.Id)
                    .ToList();

            var existingCatalogs =
                await _db.ProductCatalogs
                    .Include(x => x.PackComponents)
                    .Where(x => serverIds.Contains(x.Id))
                    .ToDictionaryAsync(
                        x => x.Id,
                        cancellationToken);

            foreach (var serverCatalog in serverCatalogs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateCatalogBarcode(serverCatalog);

                if (!existingCatalogs.TryGetValue(
                        serverCatalog.Id,
                        out var localCatalog))
                {
                    localCatalog =
                        new LocalProductCatalog
                        {
                            Id = serverCatalog.Id
                        };

                    await _db.ProductCatalogs.AddAsync(
                        localCatalog,
                        cancellationToken);

                    existingCatalogs[serverCatalog.Id] =
                        localCatalog;
                }

                MapToLocal(
                    serverCatalog,
                    localCatalog,
                    syncStartedAtUtc);

                SyncPackComponents(
                    serverCatalog,
                    localCatalog);
            }

            await _db.SaveChangesAsync(
                cancellationToken);
        }

        private static void SyncPackComponents(
    ProductCatalogResult source,
    LocalProductCatalog destination)
        {
            var serverComponents = source.IsPack
                ? source.PackComponents?
                    .Where(x =>
                        x.ComponentCatalogId != Guid.Empty &&
                        x.Quantity > 0)
                    .GroupBy(x => x.ComponentCatalogId)
                    .Select(x => x.Last())
                    .ToList()
                    ?? new List<PackComponentResult>()
                : new List<PackComponentResult>();

            var serverComponentIds = serverComponents
                .Select(x => x.ComponentCatalogId)
                .ToHashSet();

            var removedComponents = destination.PackComponents
                .Where(x =>
                    !serverComponentIds.Contains(
                        x.ComponentCatalogId))
                .ToList();

            foreach (var removedComponent in removedComponents)
            {
                destination.PackComponents.Remove(
                    removedComponent);
            }

            var existingComponents = destination.PackComponents
                .ToDictionary(x => x.ComponentCatalogId);

            foreach (var serverComponent in serverComponents)
            {
                if (!existingComponents.TryGetValue(
                        serverComponent.ComponentCatalogId,
                        out var localComponent))
                {
                    localComponent = new LocalPackComponent
                    {
                        ProductCatalogId = destination.Id,
                        ComponentCatalogId =
                            serverComponent.ComponentCatalogId
                    };

                    destination.PackComponents.Add(
                        localComponent);

                    existingComponents[
                        serverComponent.ComponentCatalogId] =
                        localComponent;
                }

                localComponent.ComponentName =
                    string.IsNullOrWhiteSpace(
                        serverComponent.ComponentName)
                        ? "Unknown product"
                        : serverComponent.ComponentName;

                localComponent.Quantity =
                    serverComponent.Quantity;
            }
        }

        private async Task MarkMissingCatalogsAsDeletedAsync(
            List<Guid> deletedIds, DateTime syncStartedAtUtc,
            CancellationToken cancellationToken)
        {
            await _db.ProductCatalogs
                .Where(x =>
                    !x.IsDeleted &&
                    deletedIds.Contains(x.Id))
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            x => x.IsDeleted,
                            true)
                        .SetProperty(
                            x => x.LastSyncedAtUtc,
                            syncStartedAtUtc),
                    cancellationToken);
        }

        private static void ValidateCatalogBarcode(
    ProductCatalogResult catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            if (string.IsNullOrWhiteSpace(catalog.Barcode))
                return;

            var barcode = catalog.Barcode.Trim();
            var internalCode = catalog.InternalCode?.Trim();

            // Only validate our internally generated barcodes.
            if (!barcode.StartsWith(
                    "100000",
                    StringComparison.Ordinal))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(internalCode))
            {
                throw new InvalidOperationException(
                    $"Catalog '{catalog.Id}' has internal barcode " +
                    $"'{barcode}' but no InternalCode.");
            }

            if (internalCode.Length > 5 ||
                !internalCode.All(char.IsDigit))
            {
                throw new InvalidOperationException(
                    $"Catalog '{catalog.Id}' contains invalid " +
                    $"InternalCode '{internalCode}'.");
            }

            var expectedBarcode =
                $"10000000{internalCode.PadLeft(5, '0')}";

            if (!string.Equals(
                    barcode,
                    expectedBarcode,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Catalog barcode inconsistency. " +
                    $"CatalogId='{catalog.Id}', " +
                    $"InternalCode='{internalCode}', " +
                    $"Barcode='{barcode}', " +
                    $"Expected='{expectedBarcode}'.");
            }
        }

        private static void MapToLocal(
            ProductCatalogResult source,
            LocalProductCatalog destination,
            DateTime syncedAtUtc)
        {
            destination.Barcode = source.Barcode;
            destination.InternalCode = source.InternalCode;
            destination.Name = source.Name;
            destination.Brand = source.Brand;
            destination.Manufacturer = source.Manufacturer;
            destination.Description = source.Description;
            destination.CategoryId = source.CategoryId;
            destination.SellingMode = source.SellingMode;

            destination.UnitOfMeasure =
                string.IsNullOrWhiteSpace(source.UnitOfMeasure)
                    ? "pcs"
                    : source.UnitOfMeasure.Trim();

            destination.IsPack = source.IsPack;
            destination.IsDeleted = false;
            destination.LastSyncedAtUtc = syncedAtUtc;
        }
    }
}
