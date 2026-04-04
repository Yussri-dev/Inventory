using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.ProductCatalogs.Requests
{
    public class CreateProductCatalogRequest
    {
        [Required, MaxLength(100)]
        public string Barcode { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(100)]
        public string? Manufacturer { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
        [MaxLength(10)]
        public string UnitOfMeasure { get; set; } = "pcs"; // pcs, kg, g, l

        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
