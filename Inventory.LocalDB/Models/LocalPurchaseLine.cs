using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalPurchaseLine : ILocalTenantEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }
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
        public decimal DiscountPercent { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [NotMapped]
        public decimal EffectiveUnitPurchasePrice =>
    Math.Round(
        UnitPurchasePrice *
        (1m - DiscountPercent / 100m),
        2);

        [NotMapped]
        public decimal LineAmountExclVat =>
            Math.Round(
                QuantityReceived *
                EffectiveUnitPurchasePrice,
                2);

        [NotMapped]
        public decimal VatAmount =>
            Math.Round(
                LineAmountExclVat *
                (VatRate / 100m),
                2);

        [NotMapped]
        public decimal LineAmountInclVat =>
            LineAmountExclVat + VatAmount;
    }
}