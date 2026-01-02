using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.InventoryLines.Results
{
    public class InventoryLineResult
    {
        public Guid Id { get; set; }

        public Guid InventorySessionId { get; set; }

        public Guid ProductId { get; set; }

        public decimal SystemQuantity { get; set; }

        public decimal CountedQuantity { get; set; }

        public decimal Variance => CountedQuantity - SystemQuantity;

        public decimal VarianceValue { get; set; }

        public DateTime? CountedAt { get; set; }

        public string? Notes { get; set; }

        public bool IsAdjusted { get; set; }
        public DateTime? AdjustedAt { get; set; }
    }
}
