using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.Sales.Requests
{
    public class CreateSaleRequest
    {
        [Required, MaxLength(100)]
        public string InvoiceNumber { get; set; } = null!;

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

        [Required]
        public SaleStatus Status { get; set; }

        [Required]
        public PaymentStatus PaymentStatus { get; set; }

        public Guid? CustomerId { get; set; }

        [Required]
        public Guid CashSessionId { get; set; }

        public DateTime SaleDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
