using Inventory.LocalDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Services.Results
{
    public sealed class ClaimedSyncBatchResult
    {
        public Guid BatchId { get; set; }

        public Guid TenantId { get; set; }

        public DateTime ClaimedAtUtc { get; set; }

        public List<SyncQueueItem> Items { get; set; } = new();

        public int Count =>
            Items.Count;

        public bool IsEmpty =>
            Items.Count == 0;
    }
}
