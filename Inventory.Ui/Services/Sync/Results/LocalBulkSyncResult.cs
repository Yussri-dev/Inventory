
namespace Inventory.Ui.Services.Sync.Results
{
    public sealed class LocalBulkSyncResult
    {
        public int BatchesProcessed { get; set; }

        public int Claimed { get; set; }

        public int Done { get; set; }

        public int Duplicates { get; set; }

        public int Failed { get; set; }

        public int Conflicts { get; set; }

        public bool WasCancelled { get; set; }

        public bool HasErrors =>
            Failed > 0 ||
            Conflicts > 0;

        public List<string> Messages { get; set; } =
            new();
    }
}
