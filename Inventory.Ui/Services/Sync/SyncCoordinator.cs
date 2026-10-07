using Microsoft.EntityFrameworkCore;
using Inventory.LocalDB.Services.Interfaces;
using Inventory.Ui.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Inventory.Ui.Services.Sync
{
    public sealed class SyncCoordinator
        : IAsyncDisposable
    {
        private const int BatchSize =
            250;

        private const int MaximumBatchesPerRun =
            20;

        private static readonly string[] ReferenceEntityNames = { "Customer", "Supplier", "Product", "StockMovement", "Damage" };
        private static readonly string[] SaleEntityNames = { "Sale" };
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SyncCoordinator> _logger;
        private readonly Channel<bool> _signals = Channel.CreateBounded<bool>(1);
        private readonly CancellationTokenSource _shutdownSource = new();
        private readonly object _lifecycleLock = new();
        private readonly SemaphoreSlim _syncLock = new(1, 1);
        private Task? _runner;
        private Task? _poller;
        private bool _disposed;
        private int _isSynchronizing;
        public bool IsSynchronizing => Volatile.Read(ref _isSynchronizing) == 1;

        public SyncCoordinator(IServiceScopeFactory scopeFactory, ILogger<SyncCoordinator> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void RequestSync() => NotifyPendingWork();
        public void NotifyPendingWork()
        {
            lock (_lifecycleLock)
            {
                if (_disposed) return;
                if (_runner == null)
                {
                    Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
                    _runner = RunAsync(_shutdownSource.Token);
                    _poller = PollAsync(_shutdownSource.Token);
                }
                _signals.Writer.TryWrite(true);
            }
        }

        private async Task PollAsync(CancellationToken ct)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            while (await timer.WaitForNextTickAsync(ct)) _signals.Writer.TryWrite(true);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var signal in _signals.Reader.ReadAllAsync(ct))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var tenant = scope.ServiceProvider.GetRequiredService<ILocalTenantContext>();
                        if (!tenant.HasTenant) continue;
                        var tenantId = tenant.GetRequiredTenantId();
                        var key = $"daily-sync-21-{tenantId:N}";
                        var now = DateTime.Now;
                        var today = DateOnly.FromDateTime(now);
                        var enabledText = Preferences.Default.Get(key + "-enabled", "");
                        if (!DateOnly.TryParseExact(enabledText, "yyyy-MM-dd", out var enabled))
                        {
                            enabled = today;
                            Preferences.Default.Set(key + "-enabled", enabled.ToString("yyyy-MM-dd"));
                        }
                        DateOnly? last = DateOnly.TryParseExact(Preferences.Default.Get(key + "-success", ""), "yyyy-MM-dd", out var parsed) ? parsed : null;
                        var due = Inventory.LocalDB.Services.DailySyncSchedule.GetDueDate(now, enabled, last);
                        if (due == null || Connectivity.Current.NetworkAccess != NetworkAccess.Internet) continue;
                        var retryText = Preferences.Default.Get(key + "-retry", "");
                        if (!Inventory.LocalDB.Services.DailySyncSchedule.CanRetry(DateTimeOffset.UtcNow,
                            DateTimeOffset.TryParse(retryText, out var retryAt) ? retryAt : null)) continue;
                        if (!await _syncLock.WaitAsync(0, ct)) continue;
                        try
                        {
                            if (tenant.TenantId != tenantId) continue;
                            // Persist a bounded retry delay, not a successful daily run.
                            Preferences.Default.Set(key + "-retry", DateTimeOffset.UtcNow.AddMinutes(5).ToString("O"));
                            await SynchronizeCoreAsync(ct);
                            Preferences.Default.Set(key + "-success", due.Value.ToString("yyyy-MM-dd"));
                            Preferences.Default.Remove(key + "-retry");
                        }
                        finally { _syncLock.Release(); }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch (Exception error) { _logger.LogWarning(error, "Daily synchronization failed; manual retry remains available."); }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        }

        public async Task SynchronizeAllAsync(CancellationToken ct = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _shutdownSource.Token);
            await _syncLock.WaitAsync(linked.Token);
            try { await SynchronizeCoreAsync(linked.Token); }
            finally { _syncLock.Release(); }
        }

        private async Task SynchronizeCoreAsync(CancellationToken ct)
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                throw new InvalidOperationException("Pas de connexion Internet. Les opérations restent enregistrées sur ce poste.");
            using var scope = _scopeFactory.CreateScope();
            var tenantId = scope.ServiceProvider.GetRequiredService<ILocalTenantContext>().GetRequiredTenantId();
            Interlocked.Exchange(ref _isSynchronizing, 1);
            try
            {
                var errors = new List<string>();
                try { await ExecuteSynchronizationAsync(ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { errors.Add("Envoi/corrections : " + ex.Message); }
                var db = scope.ServiceProvider.GetRequiredService<Inventory.LocalDB.Context.PosLocalDbContext>();
                var remaining = await db.SyncQueueItems.CountAsync(x => x.TenantId == tenantId &&
                    x.Status != "Done" && x.Status != "Draft", ct);
                if (remaining > 0) errors.Add($"{remaining} opération(s) restent en attente ou en conflit.");
                try { await scope.ServiceProvider.GetRequiredService<LocalDataBootstrapService>().RefreshAllInBackgroundAsync(ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { errors.Add("Réception : " + ex.Message); }
                // The stock pull protects every product with an unfinished local operation.
                try { await scope.ServiceProvider.GetRequiredService<ILocalStockSyncService>().FullSyncAsync(ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { errors.Add("Stocks : " + ex.Message); }
                if (errors.Count > 0) throw new InvalidOperationException("Synchronisation partielle : " + string.Join(" | ", errors));
            }
            finally { Interlocked.Exchange(ref _isSynchronizing, 0); }
        }
        private async Task ExecuteSynchronizationAsync(
    CancellationToken cancellationToken)
        {
            Exception? uploadError = null;
            try
            {
                await using (var repairScope = _scopeFactory.CreateAsyncScope())
                    await ActivatorUtilities.CreateInstance<IdentityReconciliationService>(repairScope.ServiceProvider).ReconcileAsync(cancellationToken);
                var previous = int.MaxValue;
                while (true)
                {
                    var blockingReason = await UploadAndReconcileAsync(cancellationToken);
                    await using var checkScope = _scopeFactory.CreateAsyncScope();
                    var tenantId = checkScope.ServiceProvider.GetRequiredService<ILocalTenantContext>().GetRequiredTenantId();
                    var db = checkScope.ServiceProvider.GetRequiredService<Inventory.LocalDB.Context.PosLocalDbContext>();
                    var remaining = await db.SyncQueueItems.CountAsync(x => x.TenantId == tenantId &&
                        x.Status != "Done" && x.Status != "Draft", cancellationToken);
                    if (remaining == 0 || remaining >= previous)
                    {
                        if (blockingReason != null) throw new InvalidOperationException("Synchronisation partielle : " + blockingReason);
                        break;
                    }
                    previous = remaining;
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error) { uploadError = error; }
            finally
            {
                // A fresh context prevents failed uploads from leaking tracked writes into
                // the download. The applier protects related pending operations individually.
                if (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await using var downloadScope = _scopeFactory.CreateAsyncScope();
                        if (downloadScope.ServiceProvider.GetRequiredService<ILocalTenantContext>().HasTenant)
                        {
                            var deferred = await downloadScope.ServiceProvider.GetRequiredService<SaleCustomerCorrectionSyncService>().PullAsync(cancellationToken);
                            if (deferred > 0) throw new InvalidOperationException($"Synchronisation partielle : {deferred} vente(s) attendent la résolution de modifications locales avant de recevoir les corrections serveur.");
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
                    catch (Exception error)
                    {
                        _logger.LogError(error, "Server-to-SQLite correction download failed; it will be retried on the next synchronization.");
                        if (uploadError != null) throw new AggregateException(uploadError.Message + " | " + error.Message, uploadError, error);
                        throw;
                    }
                }
            }
            if (uploadError != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(uploadError).Throw();
        }

        private async Task<string?> UploadAndReconcileAsync(CancellationToken cancellationToken)
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            /*
             * Phase 1:
             * Cash-session creation and legacy prerequisites.
             */
            var prerequisiteUploader =
                scope.ServiceProvider
                    .GetRequiredService<ILocalSyncUploader>();

            var prerequisiteResult =
                await prerequisiteUploader.SyncPendingAsync(
                    cancellationToken);

            /*
             * Do not continue when the API is unavailable, the local
             * database is locked, or a prerequisite operation failed.
             */
            if (prerequisiteResult.Failed > 0 ||
                prerequisiteResult.Skipped > 0)
            {
                _logger.LogWarning(
                    "Synchronization stopped after the prerequisite phase. " +
                    "Failed={Failed}, Skipped={Skipped}, Messages={Messages}.",
                    prerequisiteResult.Failed,
                    prerequisiteResult.Skipped,
                    string.Join(
                        " | ",
                        prerequisiteResult.Messages));

                return DescribeFailure("Ouverture des caisses", prerequisiteResult.Messages);
            }

            /*
             * Phase 2:
             * Reference entities required by purchases and sales.
             */
            var bulkUploader =
                scope.ServiceProvider
                    .GetRequiredService<ILocalBulkSyncUploader>();

            var referenceResult =
                await bulkUploader.SyncPendingAsync(
                    ReferenceEntityNames,
                    batchSize:
                        BatchSize,
                    maximumBatches:
                        MaximumBatchesPerRun,
                    cancellationToken:
                        cancellationToken);

            if (referenceResult.Failed > 0 ||
                referenceResult.Conflicts > 0)
            {
                _logger.LogWarning(
                    "Synchronization stopped after the reference phase. " +
                    "Failed={Failed}, Conflicts={Conflicts}, " +
                    "Messages={Messages}.",
                    referenceResult.Failed,
                    referenceResult.Conflicts,
                    string.Join(
                        " | ",
                        referenceResult.Messages));

                return DescribeFailure("Clients, fournisseurs, produits et stocks", referenceResult.Messages);
            }

            var purchaseResult =
                await prerequisiteUploader.SyncPurchasesAsync(cancellationToken);

            if (purchaseResult.Failed > 0 || purchaseResult.Skipped > 0)
            {
                _logger.LogWarning(
                    "Synchronization stopped after the purchase phase. " +
                    "Failed={Failed}, Skipped={Skipped}, Messages={Messages}.",
                    purchaseResult.Failed,
                    purchaseResult.Skipped,
                    string.Join(" | ", purchaseResult.Messages));

                return DescribeFailure("Achats", purchaseResult.Messages);
            }

            /*
             * Phase 3:
             * Sales must be synchronized before standalone customer
             * transactions and cash-session closure.
             */
            var saleResult =
                await bulkUploader.SyncPendingAsync(
                    SaleEntityNames,
                    batchSize:
                        BatchSize,
                    maximumBatches:
                        MaximumBatchesPerRun,
                    cancellationToken:
                        cancellationToken);

            if (saleResult.Failed > 0 ||
                saleResult.Conflicts > 0)
            {
                _logger.LogWarning(
                    "Synchronization stopped after the sale phase. " +
                    "Failed={Failed}, Conflicts={Conflicts}, " +
                    "Messages={Messages}.",
                    saleResult.Failed,
                    saleResult.Conflicts,
                    string.Join(
                        " | ",
                        saleResult.Messages));

                return DescribeFailure("Ventes", saleResult.Messages);
            }

            /*
             * Phase 4:
             * Standalone customer payments and refunds.
             */
            LocalReturnUploadResult returnResult;
            do
            {
                returnResult = await scope.ServiceProvider.GetRequiredService<ILocalReturnUploadService>().SyncPendingAsync(cancellationToken);
                if (returnResult.Failed > 0 || returnResult.Skipped > 0) return DescribeFailure("Retours", returnResult.Messages);
            } while (returnResult.TotalPending == 100);

            var customerTransactionUploader =
                scope.ServiceProvider
                    .GetRequiredService<
                        ILocalCustomerTransactionUploadService>();

            var customerTransactionResult =
                await customerTransactionUploader
                    .UploadPendingAsync(
                        cancellationToken);

            if (customerTransactionResult.Failed > 0 || customerTransactionResult.Skipped > 0)
            {
                _logger.LogWarning(
                    "Synchronization stopped after the customer-transaction " +
                    "phase. Failed={Failed}, Skipped={Skipped}, " +
                    "Messages={Messages}.",
                    customerTransactionResult.Failed,
                    customerTransactionResult.Skipped,
                    string.Join(
                        " | ",
                        customerTransactionResult.Messages));

                return DescribeFailure("Paiements clients", customerTransactionResult.Messages);
            }

            /*
             * Phase 5:
             * Pull authoritative stock.
             */
            var localDb = scope.ServiceProvider.GetRequiredService<Inventory.LocalDB.Context.PosLocalDbContext>();
            var currentTenant = scope.ServiceProvider.GetRequiredService<ILocalTenantContext>().GetRequiredTenantId();
            if (await localDb.SyncQueueItems.AnyAsync(x => x.TenantId == currentTenant &&
                x.EntityName != "CashSession" && x.Status != "Done" && x.Status != "Draft", cancellationToken)) return await DescribePendingAsync(localDb, currentTenant, cancellationToken);

            /*
             * Phase 6:
             * Close sessions only after all dependent operations.
             */
            var closureResult =
                await prerequisiteUploader
                    .SyncCashSessionClosuresAsync(
                        cancellationToken);

            if (closureResult.Failed > 0 ||
                closureResult.Skipped > 0)
            {
                _logger.LogWarning(
                    "Cash-session closure was not fully completed. " +
                    "Failed={Failed}, Skipped={Skipped}, Messages={Messages}.",
                    closureResult.Failed,
                    closureResult.Skipped,
                    string.Join(
                        " | ",
                        closureResult.Messages));
            }
            return closureResult.Failed == 0 && closureResult.Skipped == 0 ? null : DescribeFailure("Clôture des caisses", closureResult.Messages);
        }

        private static string DescribeFailure(string phase, IEnumerable<string> messages)
        {
            var details = messages.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Take(5).ToArray();
            return phase + " : " + (details.Length == 0
                ? "opération échouée ou différée ; consultez la file de synchronisation."
                : string.Join(" | ", details));
        }

        private static async Task<string> DescribePendingAsync(
            Inventory.LocalDB.Context.PosLocalDbContext db, Guid tenantId, CancellationToken ct)
        {
            var pending = db.SyncQueueItems.AsNoTracking().Where(x => x.TenantId == tenantId && x.Status != "Done" && x.Status != "Draft");
            var groups = await pending.GroupBy(x => new { x.EntityName, x.Status })
                .Select(g => new { g.Key.EntityName, g.Key.Status, Count = g.Count() }).ToListAsync(ct);
            var errors = await pending.Where(x => x.ErrorMessage != null && x.ErrorMessage != "")
                .OrderByDescending(x => x.LastAttemptAtUtc).Take(3)
                .Select(x => x.EntityName + " : " + x.ErrorMessage).ToListAsync(ct);
            return string.Join(" ; ", groups.Select(x => $"{x.EntityName} : {x.Count} ({x.Status})"))
                + ". " + string.Join(" | ", errors);
        }
        private void OnConnectivityChanged(
            object? sender,
            ConnectivityChangedEventArgs args)
        {
            if (args.NetworkAccess ==
                NetworkAccess.Internet)
            {
                NotifyPendingWork();
            }
        }

        private static async Task IgnoreCancellationAsync(
            Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async ValueTask DisposeAsync()
        {
            Task? runner;

            lock (_lifecycleLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed =
                    true;

                if (_runner != null)
                {
                    Connectivity.Current
                        .ConnectivityChanged -=
                            OnConnectivityChanged;
                }

                _shutdownSource.Cancel();

                _signals.Writer.TryComplete();

                runner =
                    _runner;
            }

            if (runner != null)
            {
                try
                {
                    await runner;
                }
                catch (OperationCanceledException)
                {
                }
            }

            if (_poller != null) await IgnoreCancellationAsync(_poller);
            _shutdownSource.Dispose();
        }
    }
}
