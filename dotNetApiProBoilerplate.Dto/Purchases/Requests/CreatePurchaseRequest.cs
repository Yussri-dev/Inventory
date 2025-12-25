
using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.Purchases.Requests
{
    public class CreatePurchaseRequest
    {
        [Required]
        public Guid SupplierId { get; set; }

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
    }
}
