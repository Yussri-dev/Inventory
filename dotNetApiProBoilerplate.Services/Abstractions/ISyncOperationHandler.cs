using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;


namespace Inventory.Services.Abstractions
{
    public interface ISyncOperationHandler
    {
        string EntityName { get; }

        Task<SyncBatchItemResult> ProcessAsync(
            SyncBatchOperationRequest operation,
            CancellationToken cancellationToken = default);
    }
}
