using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class PurchaseLine
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PurchaseId { get; set; }

        [ForeignKey(nameof(PurchaseId))]
        public Purchase Purchase { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        [Range(0, double.MaxValue)]
        public decimal QuantityOrdered { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityReceived { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal UnitPurchasePrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineAmountExclVat => QuantityReceived * UnitPurchasePrice;

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount => LineAmountExclVat * (VatRate / 100);

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineAmountInclVat => LineAmountExclVat + VatAmount;
    }
}
