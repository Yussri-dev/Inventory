using Inventory.Dto.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.PurchasePayments.Requests
{
    public class CreatePurchasePaymentRequest
    {
        [Required]
        public Guid PurchaseId { get; set; }

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
