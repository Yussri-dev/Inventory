
namespace Inventory.Dto.Analytics.Results
{
    public class LossProductResult
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal ReturnedQuantity { get; set; }
        public decimal LostRevenue { get; set; }
        public string LossReason { get; set; } = "";
    }
}
