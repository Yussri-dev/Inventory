using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.Products.Requests
{
    public class CreateProductRequest
    {
        public Guid? CatalogProductId { get; set; }

        [Required]
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

        [Range(0, double.MaxValue)]
        public decimal SalePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PurchasePrice { get; set; }

        [Range(0, 100)]
        public decimal VatRate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinStockLevel { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MaxStockLevel { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        public ProductStatus IsActive { get; set; } = ProductStatus.Active;

        public bool IsTracked { get; set; } = true;

        [MaxLength(500)]
        //[Url]
        public string? ImageUrl { get; set; }
    }
}
