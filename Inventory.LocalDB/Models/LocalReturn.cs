using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalReturn
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ServerId { get; set; }

        public Guid ClientOperationId { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string LocalReturnNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ServerReturnNumber { get; set; }

        [Required]
        public Guid LocalSaleId { get; set; }

        public Guid? ServerSaleId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required, MaxLength(50)]
        public string RefundMethod { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Reason { get; set; }

        public DateTime ReturnDateUtc { get; set; } = DateTime.UtcNow;

        public bool IsProcessed { get; set; }

        public DateTime? ProcessedAtUtc { get; set; }

        [Required, MaxLength(50)]
        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastSyncedAtUtc { get; set; }

        public ICollection<LocalReturnLine> Lines { get; set; } = new List<LocalReturnLine>();
    }
}