namespace Inventory.Ui.Services.Sync
{
    public interface ITenantStoreProfileSyncService
    {
        Task SynchronizeAsync(
            CancellationToken cancellationToken = default);
    }
}
