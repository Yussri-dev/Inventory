using Inventory.Dto.Enums;

namespace Inventory.Dto.Products.Results
{
    public class ProductResult
    {
        // Identité
        public Guid Id { get; init; }

        // Core info
        public string Name { get; init; } = null!;
        public string? Description { get; init; }

        public string? Sku { get; init; }
        public string? Barcode { get; init; }

        public string? Category { get; init; }
        public string? Brand { get; init; }

        // Pricing
        public decimal SalePrice { get; init; }
        public decimal PurchasePrice { get; init; }
        public decimal VatRate { get; init; }

        // Stock configuration (pas le stock réel)
        public decimal MinStockLevel { get; init; }
        public decimal MaxStockLevel { get; init; }

        public string? Unit { get; init; }

        // Flags
        public ProductStatus Status { get; init; }
        public bool IsTracked { get; init; }

        // Media
        public string? ImageUrl { get; init; }
    }
}
