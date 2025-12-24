using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class SaleLine
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SaleId { get; set; }

        [ForeignKey(nameof(SaleId))]
        public Sale Sale { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal DiscountPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineAmountExclVat => (UnitPrice * Quantity) - DiscountAmount;

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount => LineAmountExclVat * (VatRate / 100);

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineAmountInclVat => LineAmountExclVat + VatAmount;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
