using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalSaleLine : ILocalTenantEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TenantId { get; set; }
        public Guid LocalSaleId { get; set; }

        public LocalSale LocalSale { get; set; } = null!;

        // Product scanned/sold
        public Guid ProductLocalId { get; set; }

        public Guid? ProductServerId { get; set; }

        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ProductBarcode { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        // Product that affects stock
        public Guid UnitProductLocalId { get; set; }

        public Guid UnitProductServerId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal UnitQuantity { get; set; }

        public bool IsPack { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal UnitsPerPack { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCostPrice { get; set; }

        [NotMapped]
        public decimal LineTTC => (UnitPrice * Quantity) - DiscountAmount;

        [NotMapped]
        public decimal LineHT
        {
            get
            {
                var divisor = 1 + (VatRate / 100m);
                return divisor == 0 ? LineTTC : LineTTC / divisor;
            }
        }

        [NotMapped]
        public decimal VatAmount => LineTTC - LineHT;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
