using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;


namespace Inventory.Services.Abstractions
{
    public interface ISyncBatchOperationExecutor
    {
        Task<IReadOnlyList<SyncBatchItemResult>> ExecuteAsync(
            Guid batchId,
            IReadOnlyList<SyncBatchOperationRequest> operations,
            ISyncBatchOperationHandler handler,
            CancellationToken cancellationToken = default);
    }
}
