
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory.Dto.SaleLines.Results
{
    public class SaleLineResult
    {
        public Guid Id { get; set; }

        public Guid SaleId { get; set; }

        public Guid ProductId { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal VatRate { get; set; }

        public decimal LineAmountExclVat => (UnitPrice * Quantity) - DiscountAmount;

        public decimal VatAmount => LineAmountExclVat * (VatRate / 100);

        public decimal LineAmountInclVat => LineAmountExclVat + VatAmount;

        public string? Notes { get; set; }
    }
}
