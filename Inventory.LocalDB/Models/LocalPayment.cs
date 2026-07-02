using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalPayment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ServerId { get; set; }

        [Required]
        public Guid LocalSaleId { get; set; }

        public LocalSale LocalSale { get; set; } = null!;

        public Guid? ServerSaleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Method { get; set; } = string.Empty;
        // Cash, Card, Credit, Mixed, BankTransfer

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(200)]
        public string? TransactionRef { get; set; }

        [MaxLength(100)]
        public string? CardLastFourDigits { get; set; }

        public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsRefunded { get; set; }

        public DateTime? RefundedAtUtc { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(50)]
        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;

        public DateTime? LastSyncedAtUtc { get; set; }
    }
}
