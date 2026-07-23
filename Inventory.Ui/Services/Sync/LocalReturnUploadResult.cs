namespace Inventory.Ui.Services.Sync
{
    public sealed class LocalReturnUploadResult
    {
        public int TotalPending { get; set; }

        public int Synced { get; set; }

        public int Skipped { get; set; }

        public int Failed { get; set; }

        public List<string> Messages { get; } = new();
    }

}
