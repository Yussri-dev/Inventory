namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalSalesHistoryPaymentResult
    {
        public Guid LocalId { get; set; }

        public string Method { get; set; } =
            string.Empty;

        public decimal Amount { get; set; }

        public DateTime PaidAtUtc { get; set; }

        public string? TransactionReference { get; set; }

        public string SyncStatus { get; set; } =
            string.Empty;
    }
}
