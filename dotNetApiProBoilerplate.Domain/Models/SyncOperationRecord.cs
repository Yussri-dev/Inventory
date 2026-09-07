using Inventory.Domain.Abstraction;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Models
{
    public sealed class SyncOperationRecord
        : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid BatchId { get; set; }

        public Guid QueueItemId { get; set; }

        public Guid ClientOperationId { get; set; }

        public Guid LocalEntityId { get; set; }

        public Guid? ServerEntityId { get; set; }

        [MaxLength(200)]
        public string? ServerReferenceNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; } =
            string.Empty;

        [Required]
        [MaxLength(50)]
        public string Operation { get; set; } =
            string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } =
            string.Empty;

        [Required]
        [MaxLength(64)]
        public string PayloadHash { get; set; } =
            string.Empty;

        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }

        public DateTime ReceivedAtUtc { get; set; } =
            DateTime.UtcNow;

        public DateTime? ProcessedAtUtc { get; set; }
    }
}