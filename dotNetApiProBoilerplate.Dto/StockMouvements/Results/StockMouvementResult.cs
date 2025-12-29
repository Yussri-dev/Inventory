
using Inventory.Dto.Enums;

namespace Inventory.Dto.StockMouvements.Results
{
    public class StockMouvementResult
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public decimal QuantityChange { get; set; }

        public decimal QuantityBefore { get; set; }

        public decimal QuantityAfter { get; set; }

        public StockMovementType Type { get; set; }

        public Guid? ReferenceId { get; set; }

        public string? ReferenceNumber { get; set; }

        public string? Notes { get; set; }

        public DateTime MovementDate { get; set; }
    }
}
