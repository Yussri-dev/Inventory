namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalCustomerCreditResult
    {
        public Guid CustomerId { get; set; }

        public Guid? ServerCustomerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Balance { get; set; }

        public string SyncStatus { get; set; } = string.Empty;
    }

    public sealed class LocalCustomerDetailResult
    {
        public Guid CustomerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public decimal CurrentBalance { get; set; }

        public List<LocalCustomerTransactionResult> Transactions { get; set; } =
            new();

        public List<LocalCustomerSaleSummaryResult> Sales { get; set; } =
            new();

        public decimal TotalSales { get; set; }

        public decimal TotalPaid { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public sealed class LocalCustomerTransactionResult
    {
        public Guid Id { get; set; }

        public Guid? ServerId { get; set; }

        public string Type { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public decimal BalanceBefore { get; set; }

        public decimal BalanceAfter { get; set; }

        public string? Description { get; set; }

        public DateTime TransactionDate { get; set; }

        public bool IsCash { get; set; }

        public string SyncStatus { get; set; } = string.Empty;
    }

    public sealed class LocalCustomerSaleSummaryResult
    {
        public Guid LocalSaleId { get; set; }

        public Guid? ServerSaleId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime SaleDate { get; set; }

        public string PaymentStatus { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public string SyncStatus { get; set; } = string.Empty;
    }

    public sealed class LocalCustomerTransactionUploadResult
    {
        public int TotalPending { get; set; }

        public int Synced { get; set; }

        public int Skipped { get; set; }

        public int Failed { get; set; }

        public List<string> Messages { get; } = new();
    }
}
