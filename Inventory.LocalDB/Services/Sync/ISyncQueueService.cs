using Inventory.LocalDB.Models;
namespace Inventory.LocalDB.Services.Sync
{
    public interface ISyncQueueService
    {
        Task EnqueueAsync(
            string entityName,
            Guid localEntityId,
            string operation,
            string payloadJson,
            Guid? serverEntityId = null);

        Task<List<SyncQueueItem>> GetPendingAsync(int take = 50);

        Task<List<SyncQueueItem>> GetConflictsAsync();

        Task MarkProcessingAsync(Guid id);

        Task MarkDoneAsync(Guid id, Guid? serverEntityId = null);

        Task MarkFailedAsync(Guid id, string errorMessage);

        Task MarkConflictAsync(Guid id, string errorMessage);
    }
}
