using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Inventory.Dto.Sync.Requests
{
    public sealed class SyncBatchOperationRequest
    {
        public Guid QueueItemId { get; set; }

        public Guid ClientOperationId { get; set; }

        public Guid LocalEntityId { get; set; }

        public Guid? ServerEntityId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; } =
            string.Empty;

        [Required]
        [MaxLength(50)]
        public string Operation { get; set; } =
            string.Empty;

        public JsonElement Payload { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
