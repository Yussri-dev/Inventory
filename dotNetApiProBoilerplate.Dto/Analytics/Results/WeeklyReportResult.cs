
namespace Inventory.Dto.Analytics.Results
{
    public class WeeklyReportResult
    {
        public string Week { get; set; } = "";

        public decimal Revenue { get; set; }
        public decimal Expenses { get; set; }
        public decimal Profit { get; set; }

        public int SalesCount { get; set; }
        public int ReturnsCount { get; set; }

        public decimal CashOpening { get; set; }
        public decimal CashClosing { get; set; }
    }
}
