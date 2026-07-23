
namespace Inventory.LocalDB.Models
{
    public sealed class ReceiptSnapshot
    {
        public string CompanyName { get; set; } =
            string.Empty;

        public string? CompanyAddress { get; set; }

        public string? CompanyPhone { get; set; }

        public string? CompanyEmail { get; set; }

        public string? CompanyTaxNumber { get; set; }

        public string InvoiceNumber { get; set; } =
            string.Empty;

        public DateTime SaleDateUtc { get; set; }

        public string? CashierName { get; set; }

        public string CustomerName { get; set; } =
            "Walk-in customer";

        public List<ReceiptLineSnapshot> Lines { get; set; } =
            new();

        public decimal SubtotalExclVat { get; set; }

        public decimal TotalVat { get; set; }

        public decimal TotalAmount { get; set; }

        public List<ReceiptVatSnapshot> VatSummary { get; set; } =
            new();

        public List<ReceiptPaymentSnapshot> Payments { get; set; } =
            new();

        public decimal ChangeAmount { get; set; }

        public string? FooterText { get; set; }
    }
}
