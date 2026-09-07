using Inventory.Ui.Services.Sync.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.Interfaces
{
    public interface ILocalBulkSyncUploader
    {
        Task<LocalBulkSyncResult> SyncPendingAsync(
            IReadOnlyCollection<string> entityNames,
            int batchSize = 250,
            int maximumBatches = 20,
            CancellationToken cancellationToken = default);
    }
}
