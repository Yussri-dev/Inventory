using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.LocalDB.Models
{
    public class LocalProduct
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ServerId { get; set; }

        public Guid? CatalogProductId { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Sku { get; set; }

        [MaxLength(100)]
        public string? Barcode { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice2 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice3 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsTracked { get; set; } = true;

        [Column(TypeName = "decimal(18,3)")]
        public decimal LocalStockQuantity { get; set; }

        // Pack support
        public bool IsPack { get; set; }

        public Guid? UnitProductServerId { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal UnitsPerPack { get; set; } = 1;

        public DateTime? LastSyncedAtUtc { get; set; }

        public bool IsDeletedLocally { get; set; }
    }
}
