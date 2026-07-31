
namespace Inventory.LocalDB.Models
{
    public sealed class ReceiptSettings
    {
        public string DefaultCashierName { get; set; } = "POS";

        public string CurrencyCode { get; set; } = "EUR";

        public string? HeaderTagLine { get; set; }

        public string? SocialLine { get; set; }

        public string? ExtraAddressLine { get; set; }

        public string? FooterText { get; set; } = "Merci pour votre achat.";

        public int MaximumLogoSizeBytes { get; set; } = 1_048_576;
    }
}
