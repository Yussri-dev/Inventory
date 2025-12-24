using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Entities
{
    public class ProductCatalog
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

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Product> TenantProducts { get; set; } = new List<Product>();
    }
}
