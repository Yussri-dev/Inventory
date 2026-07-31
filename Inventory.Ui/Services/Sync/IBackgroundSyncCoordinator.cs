using Microsoft.Extensions.Logging;

namespace Inventory.Ui.Services.Sync
{
    public interface IBackgroundSyncCoordinator
    {
        bool isRunning { get; }
        Task StartAsync();
    }

    public sealed class BackgroundSyncCoordinator : IBackgroundSyncCoordinator
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<BackgroundSyncCoordinator> _logger;
        private int _isRunning;

        public BackgroundSyncCoordinator(IServiceScopeFactory serviceScopeFactory, ILogger<BackgroundSyncCoordinator> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public bool isRunning => Volatile.Read(ref _isRunning) == 1;

        public Task StartAsync()
        {
            if(Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            {
                _logger.LogDebug("Background synchronization is already running.");
                return Task.CompletedTask;
            }
            return RunAsync();
        }

        private async Task RunAsync()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();

                var bootstrap = scope.ServiceProvider.GetRequiredService<LocalDataBootstrapService>();

                await bootstrap.RefreshAllInBackgroundAsync();

                _logger.LogInformation("Application background synchronization completed");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Application background synchronization failed.");
            }
            finally
            {
                Interlocked.Exchange(ref _isRunning, 1);
            }
        }
    }
}
