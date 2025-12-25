namespace Inventory.Dto.CustomerTransactions.Results
{
    public class CustomerTransactionResult
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public decimal Amount { get; set; }

        public decimal BalanceBefore { get; set; }

        public decimal BalanceAfter { get; set; }

        public string Type { get; set; } = null!; // Credit, Debit, Payment, Refund

        public Guid? SaleId { get; set; }

        public string? Description { get; set; }

        public DateTime TransactionDate { get; set; }
    }
}
