
using Inventory.Domain.Abstraction;
using Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;


namespace Inventory.Domain.Entities
{
    public class ProductCategory  : GlobalEntity
    {
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = null!;

        public int DisplayOrder { get; set; }

        public string? Color { get; set; }

        public string? Icon { get; set; }

        public ICollection<ProductCatalog> Products { get; set; }
            = new List<ProductCatalog>();
    }
}
