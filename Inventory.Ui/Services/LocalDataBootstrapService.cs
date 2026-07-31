using Inventory.Ui.Services.Sync;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services;

public sealed class LocalDataBootstrapService
{
    private readonly ILocalSyncUploader _syncUploader;

    private readonly ILocalProductCategorySyncService _categorySync;
    private readonly ILocalProductCatalogSyncService _catalogSync;
    private readonly ILocalProductSyncService _productSync;
    private readonly ILocalStockSyncService _stockSync;
    private readonly ILocalCustomerSyncService _customerSync;
    private readonly ILocalSupplierSyncService _supplierSync;
    private readonly ILocalDamageSyncService _damageSync;

    private readonly ITenantStoreProfileSyncService
        _tenantStoreProfileSyncService;

    private readonly ILogger<LocalDataBootstrapService> _logger;

    public LocalDataBootstrapService(
        ILocalSyncUploader syncUploader,
        ILocalProductCategorySyncService categorySync,
        ILocalProductCatalogSyncService catalogSync,
        ILocalProductSyncService productSync,
        ILocalStockSyncService stockSync,
        ILocalDamageSyncService damageSync,
        ILocalCustomerSyncService customerSync,
        ILocalSupplierSyncService supplierSync,
        ITenantStoreProfileSyncService tenantStoreProfileSyncService,
        ILogger<LocalDataBootstrapService> logger)
    {
        _syncUploader = syncUploader;
        _categorySync = categorySync;
        _catalogSync = catalogSync;
        _productSync = productSync;
        _stockSync = stockSync;
        _damageSync = damageSync;
        _customerSync = customerSync;
        _supplierSync = supplierSync;
        _tenantStoreProfileSyncService =
            tenantStoreProfileSyncService;
        _logger = logger;
    }

    /// <summary>
    /// Synchronisation bloquante utilisée uniquement lorsque
    /// l'appareil ne possède pas encore les données minimales.
    /// </summary>
    public async Task EnsureCriticalDataAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting critical local data initialization.");

        await ExecuteRequiredStepAsync(
            "store profile",
            () => _tenantStoreProfileSyncService
                .SynchronizeAsync(cancellationToken),
            cancellationToken);

        await ExecuteRequiredStepAsync(
            "product categories",
            () => _categorySync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        await ExecuteRequiredStepAsync(
            "product catalogs",
            () => _catalogSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        await ExecuteRequiredStepAsync(
            "tenant products",
            () => _productSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        await ExecuteRequiredStepAsync(
            "stocks",
            () => _stockSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        _logger.LogInformation(
            "Critical local data initialization completed.");
    }

    /// <summary>
    /// Synchronisation complète non bloquante.
    ///
    /// Une erreur dans un module ne bloque pas les modules suivants.
    /// </summary>
    public async Task RefreshAllInBackgroundAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting background synchronization.");

        /*
         * Envoyer les opérations locales avant de récupérer
         * le stock autoritatif du serveur.
         */
        await ExecuteOptionalStepAsync(
            "pending upload queue",
            () => _syncUploader
                .SyncPendingAsync(cancellationToken),
            cancellationToken);

        await ExecuteOptionalStepAsync(
            "store profile",
            () => _tenantStoreProfileSyncService
                .SynchronizeAsync(cancellationToken),
            cancellationToken);

        await ExecuteOptionalStepAsync(
            "product categories",
            () => _categorySync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        await ExecuteOptionalStepAsync(
            "product catalogs",
            () => _catalogSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        await ExecuteOptionalStepAsync(
            "tenant products",
            () => _productSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        /*
         * Le stock est récupéré après l'upload des ventes,
         * achats, retours et ajustements en attente.
         */
        await ExecuteOptionalStepAsync(
            "stocks",
            () => _stockSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        await ExecuteOptionalStepAsync(
            "customers",
            () => _customerSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        await ExecuteOptionalStepAsync(
            "suppliers",
            () => _supplierSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        await ExecuteOptionalStepAsync(
            "damages",
            () => _damageSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        _logger.LogInformation(
            "Background synchronization completed.");
    }

    private async Task ExecuteRequiredStepAsync(
        string stepName,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Starting required synchronization step {StepName}.",
            stepName);

        try
        {
            await action();

            _logger.LogInformation(
                "Required synchronization step {StepName} completed.",
                stepName);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Required synchronization step {StepName} failed.",
                stepName);

            throw new InvalidOperationException(
                $"The required synchronization step '{stepName}' failed.",
                exception);
        }
    }

    private async Task ExecuteOptionalStepAsync(
        string stepName,
        Func<Task> action,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _logger.LogInformation(
                "Starting background synchronization step {StepName}.",
                stepName);

            await action();

            _logger.LogInformation(
                "Background synchronization step {StepName} completed.",
                stepName);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            /*
             * En arrière-plan, l'échec d'un module ne doit pas
             * arrêter toute la synchronisation.
             */
            _logger.LogWarning(
                exception,
                "Background synchronization step {StepName} failed. " +
                "The next step will continue.",
                stepName);
        }
    }
}