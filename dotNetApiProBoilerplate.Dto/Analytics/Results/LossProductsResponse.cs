
namespace Inventory.Dto.Analytics.Results
{
    public class LossProductsResponse
    {
        public decimal TotalLoss { get; set; }
        public List<LossProductResult> Items { get; set; } = new();
    }
}
