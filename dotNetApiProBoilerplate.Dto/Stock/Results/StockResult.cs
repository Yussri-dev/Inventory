
namespace Inventory.Dto.Stock.Results
{
    public class StockResult
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }

        public decimal Quantity { get; set; }

        public decimal ReservedQuantity { get; set; }

        public decimal AvailableQuantity => Quantity - ReservedQuantity;

        public DateTime LastUpdated { get; set; }
    }
}
