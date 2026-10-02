namespace Inventory.Ui.Services.Sync;

public interface IBackgroundSyncCoordinator
{
    bool isRunning { get; }
    Task StartAsync();
}

public sealed class BackgroundSyncCoordinator(SyncCoordinator coordinator) : IBackgroundSyncCoordinator
{
    public bool isRunning => coordinator.IsSynchronizing;
    public Task StartAsync()
    {
        coordinator.RequestSync();
        return Task.CompletedTask;
    }
}
