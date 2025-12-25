
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.CustomerTransactions.Requests
{
    public class UpdateCustomerTransactionRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceBefore { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; } = null!; // Credit, Debit, Payment, Refund

        public Guid? SaleId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime TransactionDate { get; set; }
    }
}
