namespace Inventory.Ui.Services.Sync
{
    public class LocalSyncUploadResult
    {
        public int TotalPending { get; set; }

        public int Synced { get; set; }

        public int Failed { get; set; }

        public int Skipped { get; set; }

        public List<string> Messages { get; set; } = new();
    }
}
