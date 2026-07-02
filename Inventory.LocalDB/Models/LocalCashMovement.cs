using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalCashMovement
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ServerId { get; set; }

        public Guid ClientOperationId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid LocalCashSessionId { get; set; }

        public LocalCashSession LocalCashSession { get; set; } = null!;

        public Guid? ServerCashSessionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;
        // CashIn, CashOut, SalePayment, Refund, Opening, Closing

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(200)]
        public string? ReferenceNumber { get; set; }

        public Guid? LocalReferenceId { get; set; }

        public Guid? ServerReferenceId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime MovementDateUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;

        public DateTime? LastSyncedAtUtc { get; set; }
    }
}
