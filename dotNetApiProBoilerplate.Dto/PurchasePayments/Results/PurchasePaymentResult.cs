using Inventory.Dto.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.PurchasePayments.Results
{
    public class PurchasePaymentResult
    {
        public Guid Id { get; set; }

        public Guid PurchaseId { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; }

        public string? TransactionRef { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? Notes { get; set; }
    }
}
