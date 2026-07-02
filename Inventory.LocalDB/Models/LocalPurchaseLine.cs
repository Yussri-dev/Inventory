using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalPurchaseLine
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid LocalPurchaseId { get; set; }

        public LocalPurchase LocalPurchase { get; set; } = null!;

        [Required]
        public Guid ProductLocalId { get; set; }

        public Guid? ProductServerId { get; set; }

        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ProductBarcode { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityOrdered { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal QuantityReceived { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPurchasePrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [NotMapped]
        public decimal LineAmountExclVat => QuantityReceived * UnitPurchasePrice;

        [NotMapped]
        public decimal VatAmount => LineAmountExclVat * (VatRate / 100m);

        [NotMapped]
        public decimal LineAmountInclVat => LineAmountExclVat + VatAmount;
    }
}