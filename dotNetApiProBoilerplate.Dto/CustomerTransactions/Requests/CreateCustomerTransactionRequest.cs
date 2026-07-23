using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.CustomerTransactions.Requests
{
    public sealed class CreateCustomerTransactionRequest
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

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public Guid? SaleId { get; set; }

        public Guid? CashSessionId { get; set; }

        public bool IsCash { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime? TransactionDateUtc { get; set; }
    }
}
