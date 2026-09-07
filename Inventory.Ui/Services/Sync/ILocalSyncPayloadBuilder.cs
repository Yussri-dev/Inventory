using Inventory.LocalDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.Services.Sync
{
    public interface ILocalSyncPayloadBuilder
    {
        string EntityName { get; }

        Task<string> BuildPayloadJsonAsync(SyncQueueItem queueItem, 
            CancellationToken cancellationToken = default);
    }
}
