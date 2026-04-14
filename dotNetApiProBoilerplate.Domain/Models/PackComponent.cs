using Inventory.Domain.Abstraction;
using Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Inventory.Domain.Models
{
    public class PackComponent : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid PackCatalaogId { get; set; }
        [ForeignKey(nameof(PackCatalaogId))]
        public ProductCatalog? PackCatalog { get; set; }

        public Guid ComponentCatalogId { get; set; }
        [ForeignKey(nameof(ComponentCatalogId))]
        public ProductCatalog? ComponentCatalog { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }
    }
}
