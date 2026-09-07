using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Models;


namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalSyncResultHandler
    {
        string EntityName { get; }

        Task ApplyAsync(
            SyncQueueItem queueItem,
            SyncBatchItemResult result,
            CancellationToken cancellationToken = default);
    }
}
