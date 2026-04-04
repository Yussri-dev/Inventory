using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.ProductCatalogs.Requests
{
    public class UpdateProductCatalogRequest
    {
        [Key]
        public Guid Id { get; set; }

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

        public string? UnitOfMeasure { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
