using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.Services.Sync
{
    public interface ILocalSyncUploader
    {
        Task<LocalSyncUploadResult> SyncPendingAsync(
            CancellationToken cancellationToken = default);
    }
}
