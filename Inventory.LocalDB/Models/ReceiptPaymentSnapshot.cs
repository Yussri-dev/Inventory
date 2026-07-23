
namespace Inventory.LocalDB.Models
{
    public sealed class ReceiptPaymentSnapshot
    {
        public string Method { get; set; } =            string.Empty;

        public decimal Amount { get; set; }

        public string? TransactionReference { get; set; }

        public DateTime PaidAtUtc { get; set; }
    }
}
