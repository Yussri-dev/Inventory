using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services.Sync
{
    public sealed class AutoSyncService : IAutoSyncService, IDisposable
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoSyncService> _logger;
        private readonly SemaphoreSlim _syncLock = new(1, 1);

        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        public bool IsRunning { get; private set; }

        public AutoSyncService(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoSyncService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void Start()
        {
            if (IsRunning)
                return;

            IsRunning = true;
            _cts = new CancellationTokenSource();

            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

            _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));

            _logger.LogInformation("Auto sync service started.");
        }

        public async Task SyncNowAsync(CancellationToken cancellationToken = default)
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                _logger.LogInformation("Auto sync skipped: no internet.");
                return;
            }

            if (!await _syncLock.WaitAsync(0, cancellationToken))
            {
                _logger.LogInformation("Auto sync skipped: another sync is already running.");
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var uploader = scope.ServiceProvider.GetRequiredService<ILocalSyncUploader>();

                var result = await uploader.SyncPendingAsync(cancellationToken);

                _logger.LogInformation(
                    "Auto sync completed. Pending: {Pending}, Synced: {Synced}, Failed: {Failed}, Skipped: {Skipped}",
                    result.TotalPending,
                    result.Synced,
                    result.Failed,
                    result.Skipped);

                foreach (var message in result.Messages)
                {
                    _logger.LogInformation("Auto sync: {Message}", message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto sync failed.");
            }
            finally
            {
                _syncLock.Release();
            }
        }

        private async Task RunLoopAsync(CancellationToken cancellationToken)
        {
            await SyncNowAsync(cancellationToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await timer.WaitForNextTickAsync(cancellationToken);

                    await SyncNowAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Auto sync loop error.");
                }
            }
        }

        private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1500);

                    if (_cts is not null && !_cts.IsCancellationRequested)
                    {
                        await SyncNowAsync(_cts.Token);
                    }
                });
            }
        }

        public void Dispose()
        {
            try
            {
                Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;

                _cts?.Cancel();
                _cts?.Dispose();

                _syncLock.Dispose();

                IsRunning = false;
            }
            catch
            {

            }
        }
    }
}
