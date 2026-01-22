using Inventory.Domain.Abstraction;
using Inventory.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class Product : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid CatalogProductId { get; set; }

        [ForeignKey(nameof(CatalogProductId))]
        public ProductCatalog? CatalogProduct { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? Sku { get; set; }

        [MaxLength(100)]
        public string? Barcode { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MinStockLevel { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MaxStockLevel { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        public ProductStatus IsActive { get; set; }
        public bool IsTracked { get; set; } = true;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        // Navigation
        public Stock? Stock { get; set; }
        public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public ICollection<SaleLine> SaleLines { get; set; } = new List<SaleLine>();
        public ICollection<PurchaseLine> PurchaseLines { get; set; } = new List<PurchaseLine>();
    }
}
