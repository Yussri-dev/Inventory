
namespace Inventory.LocalDB.Models
{
    public sealed class ReceiptLineSnapshot
    {
        public string ProductName { get; set; } =
            string.Empty;

        public string? Barcode { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal VatRate { get; set; }

        public decimal GrossAmountInclVat { get; set; }

        public decimal TotalDiscount { get; set; }

        public decimal AmountExclVat { get; set; }

        public decimal VatAmount { get; set; }

        public decimal TotalInclVat { get; set; }
    }
}
