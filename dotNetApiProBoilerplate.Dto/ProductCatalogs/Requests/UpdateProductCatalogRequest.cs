using Inventory.Dto.Enums;
using Inventory.Dto.PackComponent.Requests;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.ProductCatalogs.Requests
{
    public class UpdateProductCatalogRequest
    {
        public string? Barcode { get; set; }

        [MaxLength(50)]
        public string? InternalCode { get; set; }

        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(100)]
        public string? Manufacturer { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public decimal? DefaultSalePrice { get; set; }

        public decimal? DefaultSalePrice2 { get; set; }

        public decimal? DefaultSalePrice3 { get; set; }

        public decimal? DefaultPurchasePrice { get; set; }

        public decimal? DefaultVatRate { get; set; }

        public SellingMode? SellingMode { get; set; }

        public string? UnitOfMeasure { get; set; }

        public Guid? CategoryId { get; set; }

        public bool? IsPack { get; set; }

        public List<CreatePackComponentRequest>? PackComponents { get; set; }
    }
}