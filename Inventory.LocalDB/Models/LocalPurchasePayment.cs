using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalPurchasePayment : ILocalTenantEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }
        public Guid? ServerId { get; set; }

        [Required]
        public Guid LocalPurchaseId { get; set; }

        public LocalPurchase LocalPurchase { get; set; } = null!;

        public Guid? ServerPurchaseId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required, MaxLength(50)]
        public string Method { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TransactionRef { get; set; }

        public DateTime PaymentDateUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required, MaxLength(50)]
        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;

        public DateTime? LastSyncedAtUtc { get; set; }
    }
}