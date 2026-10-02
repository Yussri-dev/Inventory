using Inventory.Dto.Enums;

namespace Inventory.LocalDB.Services.Results
{
    public sealed class LocalSalesHistoryPaymentResult
    {
        public Guid LocalId { get; set; }

        public PaymentMethod Method { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaidAtUtc { get; set; }

        public string? TransactionReference { get; set; }

        public string SyncStatus { get; set; } =
            string.Empty;
    }
}
