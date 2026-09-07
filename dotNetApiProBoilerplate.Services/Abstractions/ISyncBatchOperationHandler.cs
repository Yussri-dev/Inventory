using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;

namespace Inventory.Services.Abstractions
{
    public interface ISyncBatchOperationHandler
    {
        string EntityName { get; }

        Task<IReadOnlyList<SyncBatchItemResult>>
            ProcessBatchAsync(
                IReadOnlyList<SyncBatchOperationRequest> operations,
                CancellationToken cancellationToken = default);
    }
}
