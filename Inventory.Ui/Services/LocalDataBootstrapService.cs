using Inventory.Ui.Services.Sync;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services;

public sealed class LocalDataBootstrapService
{
    private readonly ILocalSyncUploader _syncUploader;

    private readonly ILocalProductCategorySyncService
        _categorySync;

    private readonly ILocalProductCatalogSyncService
        _catalogSync;

    private readonly ILocalProductSyncService
        _productSync;

    private readonly ILocalStockSyncService
        _stockSync;

    private readonly ILocalCustomerSyncService
        _customerSync;

    private readonly ILogger<LocalDataBootstrapService>
        _logger;

    private readonly ILocalSupplierSyncService
        _supplierSync;

    private readonly ILocalDamageSyncService
    _damageSync;

    public LocalDataBootstrapService(
        ILocalSyncUploader syncUploader,
        ILocalProductCategorySyncService categorySync,
        ILocalProductCatalogSyncService catalogSync,
        ILocalProductSyncService productSync,
        ILocalStockSyncService stockSync,
            ILocalDamageSyncService damageSync,

        ILocalCustomerSyncService customerSync,
        ILocalSupplierSyncService supplierSync,
        ILogger<LocalDataBootstrapService> logger)
    {
        _syncUploader = syncUploader;
        _categorySync = categorySync;
        _catalogSync = catalogSync;
        _productSync = productSync;
        _stockSync = stockSync;
        _customerSync = customerSync;
        _supplierSync = supplierSync;
        _damageSync = damageSync;
        _logger = logger;
    }

    public async Task InitializeAfterLoginAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting local data bootstrap.");

        /*
         * 1. Envoyer les opérations offline.
         */
        await _syncUploader.SyncPendingAsync(
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        /*
         * 2. Synchroniser les données globales.
         */
        await _categorySync.FullSyncAsync(
            cancellationToken);

        await _catalogSync.FullSyncAsync(
            cancellationToken);

        /*
         * 3. Synchroniser les données du tenant.
         */
        await _productSync.FullSyncAsync(
            cancellationToken);

        await _customerSync.FullSyncAsync(
            cancellationToken);

        await _supplierSync.FullSyncAsync(
    cancellationToken);

        await _stockSync.FullSyncAsync(
            cancellationToken);

        await _damageSync.FullSyncAsync(
        cancellationToken);

        _logger.LogInformation(
            "Local data bootstrap completed successfully.");
    }
}