
namespace Inventory.Dto.Analytics.Results
{
    public class ProfitAnalyticsResult
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal TotalDamages { get; set; }

        public decimal GrossProfit { get; set; }
        public decimal ProfitMargin { get; set; }

        public decimal CreditRevenue { get; set; }

    }

}
