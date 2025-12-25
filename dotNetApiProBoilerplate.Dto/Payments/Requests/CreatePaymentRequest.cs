using Inventory.Dto.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
namespace Inventory.Dto.Payments.Requests
{
    public class CreatePaymentRequest
    {

        [Required]
        public Guid SaleId { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(200)]
        public string? TransactionRef { get; set; }

        [MaxLength(100)]
        public string? CardLastFourDigits { get; set; }

        public DateTime PaidAt { get; set; }

        public bool IsRefunded { get; set; }
        public DateTime? RefundedAt { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
