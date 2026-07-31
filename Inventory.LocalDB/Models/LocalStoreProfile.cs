using System.ComponentModel.DataAnnotations;

namespace Inventory.LocalDB.Models
{
    public sealed class LocalStoreProfile
    {
        public Guid TenantId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } =
            string.Empty;

        [MaxLength(200)]
        public string? LegalName { get; set; }

        [MaxLength(100)]
        public string? TradeName { get; set; }

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        [MaxLength(50)]
        public string? RegistrationNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string? Mobile { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        [MaxLength(2000)]
        public string? ReceiptHeader { get; set; }

        [MaxLength(2000)]
        public string? ReceiptFooter { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } =
            "EUR";

        [Required]
        [MaxLength(10)]
        public string CurrencySymbol { get; set; } =
            "€";

        [Required]
        [MaxLength(10)]
        public string Locale { get; set; } =
            "fr-BE";

        public DateTime LastSyncedAtUtc { get; set; }

        /*
         * Configuration spécifique du ticket.
         */
        [Required]
        [MaxLength(10)]
        public string ReceiptCurrencyCode { get; set; } =
            "EUR";

        [MaxLength(200)]
        public string? ReceiptHeaderTagLine { get; set; }

        [MaxLength(200)]
        public string? ReceiptSocialLine { get; set; }

        [MaxLength(300)]
        public string? ReceiptExtraAddressLine { get; set; }

        [MaxLength(100)]
        public string? ReceiptDefaultCashierName { get; set; }

        [MaxLength(200)]
        public string? ReceiptLogoFileName { get; set; }

        [MaxLength(100)]
        public string? ReceiptLogoContentType { get; set; }

        public byte[]? ReceiptLogoBytes { get; set; }

        public DateTime? ReceiptConfigurationUpdatedAtUtc { get; set; }
    }
}