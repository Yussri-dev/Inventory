
using Inventory.Domain.Abstraction;
using Inventory.Domain.Enums;
using Inventory.Domain.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class Sale : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

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

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [Required]
        public Guid CashSessionId { get; set; }

        [ForeignKey(nameof(CashSessionId))]
        public CashSession CashSession { get; set; } = null!;

        public DateTime SaleDate { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<SaleLine> Lines { get; set; } = new List<SaleLine>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Return> Returns { get; set; } = new List<Return>();
        public SaleReceipt? Receipt { get; set; }
    }
}
