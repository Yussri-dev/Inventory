using System.Text.Json.Serialization;

namespace Inventory.Dto.Sync.Results
{
    public sealed class SyncBatchItemResult
    {
        public Guid QueueItemId { get; set; }

        public Guid ClientOperationId { get; set; }

        public Guid? ServerEntityId { get; set; }

        public string Status { get; set; } =
            SyncBatchItemStatus.Failed;

        public string? ErrorMessage { get; set; }

        public string? ServerReferenceNumber { get; set; }

        [JsonIgnore]
        public bool IsSuccessful =>
            string.Equals(
                Status,
                SyncBatchItemStatus.Done,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                Status,
                SyncBatchItemStatus.Duplicate,
                StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool IsConflict =>
            string.Equals(
                Status,
                SyncBatchItemStatus.Conflict,
                StringComparison.OrdinalIgnoreCase);
    }
}
