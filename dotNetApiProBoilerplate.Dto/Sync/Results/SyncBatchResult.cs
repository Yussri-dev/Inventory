namespace Inventory.Dto.Sync.Results
{
    public sealed class SyncBatchResult
    {
        public Guid BatchId { get; set; }

        public DateTime ProcessedAtUtc { get; set; } =
            DateTime.UtcNow;

        public List<SyncBatchItemResult> Items { get; set; } =
            new();

        public int Total =>
            Items.Count;

        public int Succeeded =>
            Items.Count(item =>
                item.IsSuccessful);

        public int Duplicates =>
            Items.Count(item =>
                string.Equals(
                    item.Status,
                    SyncBatchItemStatus.Duplicate,
                    StringComparison.OrdinalIgnoreCase));

        public int Failed =>
            Items.Count(item =>
                string.Equals(
                    item.Status,
                    SyncBatchItemStatus.Failed,
                    StringComparison.OrdinalIgnoreCase));

        public int Conflicts =>
            Items.Count(item =>
                item.IsConflict);
    }
}
