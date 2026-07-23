
namespace Inventory.LocalDB.Models
{
    public sealed class ReceiptVatSnapshot
    {
        public decimal VatRate { get; set; }

        public decimal AmountExclVat { get; set; }

        public decimal VatAmount { get; set; }

        public decimal AmountInclVat { get; set; }
    }
}
