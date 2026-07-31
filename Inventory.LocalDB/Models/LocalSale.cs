using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public sealed class LocalSale : ILocalTenantEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }

        public Guid? ServerId { get; set; }

        public Guid ClientOperationId { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string LocalInvoiceNumber { get; set; } = string.Empty;

        public string? ReceiptBarcodeValue { get; set; }
        [MaxLength(100)]
        public string? ServerInvoiceNumber { get; set; }

        public Guid? CustomerLocalId { get; set; }

        public Guid? CustomerServerId { get; set; }

        public Guid? LocalCashSessionId { get; set; }

        public LocalCashSession? LocalCashSession { get; set; }

        public Guid? CashSessionServerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubtotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ChangeAmount { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = LocalSaleStatus.Completed;

        [Required, MaxLength(50)]
        public string PaymentStatus { get; set; } = LocalPaymentStatus.Paid;

        [Required, MaxLength(50)]
        public string SyncStatus { get; set; } = SyncQueueStatus.Pending;

        public DateTime SaleDateUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastSyncedAtUtc { get; set; }

        public ICollection<LocalSaleLine> Lines { get; set; } = new List<LocalSaleLine>();

        public ICollection<LocalPayment> Payments { get; set; } = new List<LocalPayment>();
    }
}