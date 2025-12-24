
using Inventory.Domain.Abstraction;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class CustomerTransaction : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

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
