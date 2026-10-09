
using Inventory.Dto.Enums;

namespace Inventory.Dto.ProductCatalogs.Requests
{
    public sealed class ApproveCatalogProductRequest
    {
        public string InternalCode { get; set; } = string.Empty;

        public Guid CategoryId { get; set; }

        public string? Manufacturer { get; set; }

        public SellingMode SellingMode { get; set; }
            = SellingMode.Unit;

        public string UnitOfMeasure { get; set; }
            = "pcs";
    }
}
