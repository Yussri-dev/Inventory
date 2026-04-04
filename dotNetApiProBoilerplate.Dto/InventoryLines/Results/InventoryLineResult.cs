using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.InventoryLines.Results
{
    public class InventoryLineResult
    {
        public Guid Id { get; set; }

        public Guid InventorySessionId { get; set; }

        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductBarcode { get; set; }

        public decimal SystemQuantity { get; set; }

        public decimal CountedQuantity { get; set; }

        // Computed — never stored
        public decimal Variance => CountedQuantity - SystemQuantity;

        public DateTime? CountedAt { get; set; }

        public string? Notes { get; set; }

        public bool IsAdjusted { get; set; }
        public DateTime? AdjustedAt { get; set; }
    }
}
