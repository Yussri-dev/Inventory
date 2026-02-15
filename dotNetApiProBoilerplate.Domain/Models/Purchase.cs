
using Inventory.Domain.Abstraction;
using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class Purchase : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Supplier Supplier { get; set; } = null!;

        [Required, MaxLength(100)]
        public string PurchaseNumber { get; set; } = null!;

        [MaxLength(100)]
        public string? SupplierInvoiceNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountExclVat { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountInclVat { get; set; }

        [Required]
        public PurchaseStatus Status { get; set; }

        public DateTime PurchaseDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime? PaymentDueDate { get; set; }
        public DateTime? PaymentDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<PurchaseLine> Lines { get; set; } = new List<PurchaseLine>();
        public ICollection<PurchasePayment> Payments { get; set; } = new List<PurchasePayment>();
    }
}
