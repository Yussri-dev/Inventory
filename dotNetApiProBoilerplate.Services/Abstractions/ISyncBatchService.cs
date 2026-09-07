using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
namespace Inventory.Services.Abstractions
{
    public interface ISyncBatchService
    {
        Task<SyncBatchResult> ProcessAsync(
            SyncBatchRequest request,
            CancellationToken cancellationToken = default);
    }
}
