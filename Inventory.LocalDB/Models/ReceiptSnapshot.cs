
namespace Inventory.LocalDB.Models
{
    public sealed class ReceiptSnapshot
    {
        public byte[]? LogoBytes { get; set; }

        public string? HeaderTagLine { get; set; }          // ex: SAVE MONEY . LIVE BETTER
        public string? SocialLine { get; set; }             // ex: INSTAGRAM.nasrmarket7
        public string? ExtraAddressLine { get; set; }       // ex: 21 BD EL AHRAM HAY NASR
        public string? CurrencyCode { get; set; } = "EUR"; // ou MAD

        public decimal TotalReceived { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string? CompanyAddress { get; set; }

        public string? CompanyPhone { get; set; }

        public string? CompanyEmail { get; set; }

        public string? CompanyTaxNumber { get; set; }

        public string? CompanyLegalName { get; set; }

        public string? CompanyRegistrationNumber { get; set; }

        public string? CompanyMobile { get; set; }

        public string? CompanyWebsite { get; set; }

        public string? ReceiptHeader { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;
        public string BarcodeValue { get; set; } = string.Empty;
        public DateTime SaleDateUtc { get; set; }

        public string? CashierName { get; set; }

        public string CustomerName { get; set; } = "Walk-in customer";

        public List<ReceiptLineSnapshot> Lines { get; set; } = new();

        public decimal SubtotalExclVat { get; set; }

        public decimal TotalVat { get; set; }

        public decimal TotalAmount { get; set; }

        public List<ReceiptVatSnapshot> VatSummary { get; set; } = new();

        public List<ReceiptPaymentSnapshot> Payments { get; set; } = new();

        public decimal ChangeAmount { get; set; }

        public string? FooterText { get; set; }
    }
}
