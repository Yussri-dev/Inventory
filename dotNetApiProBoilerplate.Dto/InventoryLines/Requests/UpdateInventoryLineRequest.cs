using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.InventoryLines.Requests
{
    public class UpdateInventoryLineRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid InventorySessionId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

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
