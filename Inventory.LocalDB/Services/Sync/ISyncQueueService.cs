using Inventory.Dto.Sync.Results;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Results;

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

        Task<List<SyncQueueItem>> GetPendingAsync(
            int take = 50);

        Task<List<SyncQueueItem>> GetPendingBatchAsync(
            int batchSize = 5_000,
            CancellationToken cancellationToken = default);

        Task<ClaimedSyncBatchResult> ClaimPendingBatchAsync(
            IReadOnlyCollection<string> entityNames,
            int batchSize = 5_000,
            CancellationToken cancellationToken = default);

        Task ApplyBatchResultAsync(
            Guid batchId,
            SyncBatchResult result,
            CancellationToken cancellationToken = default);

        Task ReleaseBatchAsync(
            Guid batchId,
            string errorMessage,
            CancellationToken cancellationToken = default);

        Task<List<SyncQueueItem>> GetConflictsAsync();

        Task MarkProcessingAsync(
            Guid id);

        Task MarkDoneAsync(
            Guid id,
            Guid? serverEntityId = null);

        Task MarkFailedAsync(
            Guid id,
            string errorMessage);

        Task MarkConflictAsync(
            Guid id,
            string errorMessage);
    }
}