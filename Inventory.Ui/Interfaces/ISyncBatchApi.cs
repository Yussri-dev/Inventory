using Inventory.Dto.Sync.Requests;
using Inventory.Dto.Sync.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface ISyncBatchApi
    {
        [Post("/api/sync/batches")]
        Task<SyncBatchResult> UploadAsync([Body] SyncBatchRequest request, CancellationToken cancellationToken = default);
    }
}
