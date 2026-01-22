using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Inventory.Dto.Products.Requests
{
    public class CreateProductRequest
    {
        [Required]
        public Guid CatalogProductId { get; set; }

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

        public bool IsTracked { get; set; } = true;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductStatus IsActive { get; set; } = ProductStatus.Active;
    }

}
