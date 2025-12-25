using Inventory.Dto.Enums;

namespace Inventory.Dto.Payments.Results
{
    public class PaymentResult
    {
        public Guid Id { get; set; }

        public Guid SaleId { get; set; }

        public PaymentMethod Method { get; set; }

        public decimal Amount { get; set; }

        public string? TransactionRef { get; set; }

        public string? CardLastFourDigits { get; set; }

        public DateTime PaidAt { get; set; }

        public bool IsRefunded { get; set; }
        public DateTime? RefundedAt { get; set; }

        public string? Notes { get; set; }
    }
}
