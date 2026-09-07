using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;

namespace Inventory.Services.Abstractions
{
    public interface ISyncOperationExecutor
    {
        Task<SyncBatchItemResult> ExecuteAsync(
            Guid batchId,
            SyncBatchOperationRequest operation,
            ISyncOperationHandler handler,
            CancellationToken cancellationToken = default);
    }
}
