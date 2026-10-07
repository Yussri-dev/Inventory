using Inventory.Dto.Enums;
using Inventory.Dto.PackComponent.Results;

namespace Inventory.Dto.ProductCatalogs.Results
{
    public class ProductCatalogResult
    {
        public Guid Id { get; set; }

        public string? Barcode { get; set; }

        public string InternalCode { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Brand { get; set; }

        public string? Manufacturer { get; set; }

        public string? Description { get; set; }

        public string? UnitOfMeasure { get; set; }

        public SellingMode SellingMode { get; set; }

        // =========================
        // DEFAULT PRICING
        // =========================

        public decimal DefaultSalePrice { get; set; }

        public decimal DefaultSalePrice2 { get; set; }

        public decimal DefaultSalePrice3 { get; set; }

        public decimal DefaultPurchasePrice { get; set; }

        public decimal DefaultVatRate { get; set; }

        // =========================
        // CATALOG
        // =========================

        public bool IsPack { get; set; }

        public Guid CategoryId { get; set; }

        public List<PackComponentResult> PackComponents { get; set; } = new();

        public DateTime CreatedAt { get; set; }

        public DateTime? ModifiedAt { get; set; }
    }
}