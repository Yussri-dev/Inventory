using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.PurchaseLines.Requests
{
    public class CreatePurchaseLineRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PurchaseId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

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
