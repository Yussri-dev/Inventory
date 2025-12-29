using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.PurchaseLines.Results
{
    public class PurchaseLineResult
    {
        public Guid Id { get; set; }

        public Guid PurchaseId { get; set; }

        public Guid ProductId { get; set; }

        public decimal QuantityOrdered { get; set; }

        public decimal QuantityReceived { get; set; }

        public decimal UnitPurchasePrice { get; set; }

        public decimal VatRate { get; set; }

        public decimal LineAmountExclVat => QuantityReceived * UnitPurchasePrice;

        public decimal VatAmount => LineAmountExclVat * (VatRate / 100);

        public decimal LineAmountInclVat => LineAmountExclVat + VatAmount;
    }
}
