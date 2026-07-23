using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.CustomerTransactions.Requests
{
    public sealed class RegisterCustomerPaymentRequest
    {
        [Required]
        public Guid ClientOperationId { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [Range(
            typeof(decimal),
            "0.01",
            "79228162514264337593543950335")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsCash { get; set; } = true;

        /*
         * Server cash-session id. A local service resolves this value
         * from LocalCashSession.ServerId during upload.
         */
        public Guid? CashSessionId { get; set; }

        /*
         * Original UTC operation time captured by the offline POS.
         */
        public DateTime? TransactionDateUtc { get; set; }
    }
}
