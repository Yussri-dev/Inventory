using Inventory.Dto.Enums;

namespace Inventory.Dto.Products.Results
{
    public class ProductResult
    {
        // Product instance (tenant-specific)
        public Guid Id { get; init; }

        // Link to catalog
        public Guid CatalogProductId { get; init; }

        // Optional: denormalized display (READ-ONLY)
        public string CatalogName { get; init; } = null!;
        public string? CatalogBrand { get; init; }
        public string? CatalogBarcode { get; init; }

        // Pricing (tenant-specific)
        public decimal SalePrice { get; init; }
        public decimal SalePrice2 { get; init; }
        public decimal SalePrice3 { get; init; }
        public decimal PurchasePrice { get; init; }
        public decimal VatRate { get; init; }

        // Stock configuration
        public decimal MinStockLevel { get; init; }
        public decimal MaxStockLevel { get; init; }

        // Flags
        public ProductStatus Status { get; init; }
        public bool IsTracked { get; init; }

        public bool IsPack { get; set; }
        public decimal PackSize { get; set; } = 1m;
        public Guid? ComponentProductId { get; set; }
    }

}
