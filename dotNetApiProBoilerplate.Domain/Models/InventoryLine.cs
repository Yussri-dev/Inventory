
using Inventory.Domain.Abstraction;
using Inventory.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Models
{
    public class InventoryLine : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid InventorySessionId { get; set; }

        [ForeignKey(nameof(InventorySessionId))]
        public InventorySession InventorySession { get; set; } = null!;

        [Required]
        public Guid ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        public decimal SystemQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal CountedQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Variance => CountedQuantity - SystemQuantity;

        [Column(TypeName = "decimal(18,2)")]
        public decimal VarianceValue { get; set; }

        public DateTime? CountedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public bool IsAdjusted { get; set; }
        public DateTime? AdjustedAt { get; set; }
    }

}
