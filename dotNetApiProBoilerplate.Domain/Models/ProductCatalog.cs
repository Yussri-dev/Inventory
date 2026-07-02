using Inventory.Domain.Abstraction;
using Inventory.Domain.Barcodes;
using Inventory.Domain.Enums;
using Inventory.Domain.Models;
using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Entities
{
    public class ProductCatalog : GlobalEntity
    {
        [Key]
        public Guid Id { get; set; }

        public string? Barcode { get; set; } = null!;

        [Required, MaxLength(50)]
        public string InternalCode { get; set; } = null!;

        public BarcodeType? BarcodeType { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(100)]
        public string? Manufacturer { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public new DateTime CreatedAt { get; set; }
        public new DateTime? ModifiedAt { get; set; }

        public SellingMode SellingMode {  get; set; }
        public string UnitOfMeasure { get; set; } = "pcs"; // pcs, kg, g, l

        public bool IsPack { get; set; } = false;
        public bool IsGlobal { get; set; }

        public Guid CategoryId { get; set; }
        public ProductCategory Category { get; set; }

        public ICollection<PackComponent> PackComponents { get; set; }
            = new List<PackComponent>();

        public ICollection<PackComponent> UsedInPacks { get; set; }
        = new List<PackComponent>();
        // Navigation properties
        public ICollection<Product> TenantProducts { get; set; } = new List<Product>();
    }
}
