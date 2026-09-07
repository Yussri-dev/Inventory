using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Sync.Requests
{
    public sealed class SyncBatchRequest
    {
        public Guid BatchId { get; set; }

        public DateTime CreatedAtUtc { get; set; } =
            DateTime.UtcNow;

        [Required]
        [MinLength(1)]
        [MaxLength(5000)]
        public List<SyncBatchOperationRequest> Operations { get; set; } =
            new();
    }
}
