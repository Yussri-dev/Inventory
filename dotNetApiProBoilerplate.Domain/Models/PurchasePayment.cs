
using Inventory.Domain.Abstraction;
using Inventory.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Domain.Entities
{
    public class PurchasePayment : TenantEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid PurchaseId { get; set; }

        [ForeignKey(nameof(PurchaseId))]
        public Purchase Purchase { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        [MaxLength(200)]
        public string? TransactionRef { get; set; }

        public DateTime PaymentDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
