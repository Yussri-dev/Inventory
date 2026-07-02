using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalPurchase
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? ServerId { get; set; }

        public Guid ClientOperationId { get; set; } = Guid.NewGuid();

        public Guid? SupplierLocalId { get; set; }

        public Guid? SupplierServerId { get; set; }

        [Required, MaxLength(100)]
        public string LocalPurchaseNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ServerPurchaseNumber { get; set; }

        [MaxLength(100)]
        public string? SupplierInvoiceNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountExclVat { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountInclVat { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = LocalPurchaseStatus.Draft;

        public DateTime PurchaseDateUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ExpectedDeliveryDateUtc { get; set; }

        public DateTime? DeliveryDateUtc { get; set; }

        public DateTime? PaymentDueDateUtc { get; set; }

        public DateTime? PaymentDateUtc { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required, MaxLength(50)]
        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastSyncedAtUtc { get; set; }

        public ICollection<LocalPurchaseLine> Lines { get; set; } = new List<LocalPurchaseLine>();

        public ICollection<LocalPurchasePayment> Payments { get; set; } = new List<LocalPurchasePayment>();
    }
}