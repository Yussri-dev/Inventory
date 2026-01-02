namespace Inventory.Dto.SalesSummaryDaily.Results
{
    public class SalesSummaryDailyResult
    {
        public Guid Id { get; set; }

        public DateTime Date { get; set; }

        public int TotalTransactions { get; set; }
        public int TotalItems { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal TotalVat { get; set; }

        public decimal TotalDiscount { get; set; }

        public decimal CashSales { get; set; }

        public decimal CardSales { get; set; }

        public decimal CreditSales { get; set; }

        public decimal AverageTransactionValue { get; set; }

        public DateTime GeneratedAt { get; set; }
    }
}
