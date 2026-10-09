using Inventory.Dto.Enums;

namespace Inventory.Dto.Products.Results
{
    public class ProductResult
    {
        public Guid Id { get; init; }

        public Guid? CatalogProductId { get; init; }

        public CatalogApprovalStatus CatalogApprovalStatus { get; set; }

        public string Name { get; init; } = null!;

        public string? Sku { get; init; }

        public string? Barcode { get; init; }

        public string? Description { get; init; }

        public string? Category { get; init; }

        public string? Brand { get; init; }

        public string? Unit { get; init; }

        public decimal SalePrice { get; init; }

        public decimal SalePrice2 { get; init; }

        public decimal SalePrice3 { get; init; }

        public decimal PurchasePrice { get; init; }

        public decimal VatRate { get; init; }

        public decimal MinStockLevel { get; init; }

        public decimal MaxStockLevel { get; init; }

        public ProductStatus Status { get; init; }

        public bool IsTracked { get; init; }

        public bool IsPack { get; set; }

        public decimal PackSize { get; set; } = 1m;

        public Guid? ComponentProductId { get; set; }
    }
}