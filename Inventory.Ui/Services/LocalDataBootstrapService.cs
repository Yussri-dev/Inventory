using Inventory.Ui.Services.Sync;
using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services;

public sealed class LocalDataBootstrapService
{
    private readonly Inventory.LocalDB.Context.PosLocalDbContext _db;
    private readonly ILocalSyncUploader _syncUploader;

    private readonly ILocalProductCategorySyncService _categorySync;
    private readonly ILocalProductCatalogSyncService _catalogSync;
    private readonly ILocalProductSyncService _productSync;
    private readonly ILocalStockSyncService _stockSync;
    private readonly ILocalCustomerSyncService _customerSync;
    private readonly ILocalSupplierSyncService _supplierSync;
    private readonly ILocalDamageSyncService _damageSync;

    private readonly ITenantStoreProfileSyncService _tenantStoreProfileSyncService;

    private readonly SyncCoordinator _syncCoordinator;
    private readonly ILogger<LocalDataBootstrapService> _logger;

    public LocalDataBootstrapService(
        Inventory.LocalDB.Context.PosLocalDbContext db,
        ILocalSyncUploader syncUploader,
        ILocalProductCategorySyncService categorySync,
        ILocalProductCatalogSyncService catalogSync,
        ILocalProductSyncService productSync,
        ILocalStockSyncService stockSync,
        ILocalDamageSyncService damageSync,
        ILocalCustomerSyncService customerSync,
        ILocalSupplierSyncService supplierSync,
        ITenantStoreProfileSyncService tenantStoreProfileSyncService,
        SyncCoordinator syncCoordinator,
        ILogger<LocalDataBootstrapService> logger)
    {
        _db = db;
        _syncUploader = syncUploader;
        _categorySync = categorySync;
        _catalogSync = catalogSync;
        _productSync = productSync;
        _stockSync = stockSync;
        _damageSync = damageSync;
        _customerSync = customerSync;
        _supplierSync = supplierSync;
        _tenantStoreProfileSyncService = tenantStoreProfileSyncService;
        _syncCoordinator = syncCoordinator;
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
    /// Appelée après les envois. Toute erreur remonte au coordinateur.
    /// </summary>
    public async Task RefreshAllInBackgroundAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting background synchronization.");

        var failures = new List<string>();
        async Task Pull(string name, Func<Task> action)
        {
            try { await ExecuteRequiredStepAsync(name, action, cancellationToken); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { failures.Add(name + ": " + ex.GetBaseException().Message); }
            finally { _db.ChangeTracker.Clear(); }
        }
        await Pull("store profile", () => _tenantStoreProfileSyncService.SynchronizeAsync(cancellationToken));
        await Pull("product categories", () => _categorySync.FullSyncAsync(cancellationToken));
        await Pull("product catalogs", () => _catalogSync.FullSyncAsync(cancellationToken));
        await Pull("tenant products", () => _productSync.FullSyncAsync(cancellationToken));
        await Pull("customers", () => _customerSync.FullSyncAsync(cancellationToken));
        await Pull("suppliers", () => _supplierSync.FullSyncAsync(cancellationToken));
        await Pull("damages", () => _damageSync.FullSyncAsync(cancellationToken));
        if (failures.Count > 0) throw new InvalidOperationException(string.Join(" | ", failures));

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

    public async Task ForceProductsFullSyncAsync(
     CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Starting FORCED catalog/product synchronization.");

        // 1. Categories
        await ExecuteRequiredStepAsync(
            "forced product categories",
            () => _categorySync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        _db.ChangeTracker.Clear();

        // 2. Product catalogs
        await ExecuteRequiredStepAsync(
            "forced product catalogs",
            () => _catalogSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        _db.ChangeTracker.Clear();

        // 3. Tenant products
        await ExecuteRequiredStepAsync(
            "forced tenant products",
            () => _productSync
                .ForceFullSyncAsync(cancellationToken),
            cancellationToken);

        _db.ChangeTracker.Clear();

        // 4. Stocks
        await ExecuteRequiredStepAsync(
            "forced stocks",
            () => _stockSync
                .FullSyncAsync(cancellationToken),
            cancellationToken);

        _db.ChangeTracker.Clear();

        _logger.LogWarning(
            "Forced catalog/product synchronization completed.");
    }

    public void NotifyAuthenticatedTenantReady()
    {
        _syncCoordinator.RequestSync();
    }
}
