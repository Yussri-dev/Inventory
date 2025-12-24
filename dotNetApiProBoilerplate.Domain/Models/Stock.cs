
using Inventory.Domain.Abstraction;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class Stock : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal ReservedQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal AvailableQuantity => Quantity - ReservedQuantity;

        public DateTime LastUpdated { get; set; }
    }
}
