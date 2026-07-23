namespace Inventory.Ui.Services.Sync
{
    public interface IAutoSyncService
    {
        bool IsRunning { get; }

        void Start();

        Task SyncNowAsync(CancellationToken cancellationToken = default);
    }
}
