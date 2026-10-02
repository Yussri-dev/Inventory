namespace Inventory.Ui.Services.Sync;

// Compatibility entry point: all callers share the daily scheduler and synchronization lock.
public sealed class AutoSyncService(SyncCoordinator coordinator) : IAutoSyncService, IDisposable
{
    public bool IsRunning { get; private set; }
    public void Start() { IsRunning = true; coordinator.RequestSync(); }
    public Task SyncNowAsync(CancellationToken cancellationToken = default) => coordinator.SynchronizeAllAsync(cancellationToken);
    public void Dispose() { IsRunning = false; }
}
