namespace Inventory.Dto.Analytics.Results
{
    public class TopProductResult 
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
