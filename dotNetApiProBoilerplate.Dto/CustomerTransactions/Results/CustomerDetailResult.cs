namespace Inventory.Dto.CustomerTransactions.Results
{
    public class CustomerDetailResult
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPaid { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CustomerTransactionResult> Transactions { get; set; } = new();
        public List<SaleSummaryResult> Sales { get; set; } = new();
    }
}
