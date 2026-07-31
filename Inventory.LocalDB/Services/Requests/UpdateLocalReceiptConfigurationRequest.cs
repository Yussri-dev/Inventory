using System.ComponentModel.DataAnnotations;


namespace Inventory.LocalDB.Services.Requests
{
    public sealed class UpdateLocalReceiptConfigurationRequest
    {
        [Required]
        [MaxLength(10)]
        public string CurrencyCode { get; set; } =
            "EUR";

        [MaxLength(200)]
        public string? HeaderTagLine { get; set; }

        [MaxLength(2000)]
        public string? ReceiptHeader { get; set; }

        [MaxLength(200)]
        public string? SocialLine { get; set; }

        [MaxLength(300)]
        public string? ExtraAddressLine { get; set; }

        [MaxLength(2000)]
        public string? ReceiptFooter { get; set; }

        [MaxLength(100)]
        public string? DefaultCashierName { get; set; }

        public byte[]? LogoBytes { get; set; }

        [MaxLength(200)]
        public string? LogoFileName { get; set; }

        [MaxLength(100)]
        public string? LogoContentType { get; set; }

        public bool RemoveLogo { get; set; }
    }
}
