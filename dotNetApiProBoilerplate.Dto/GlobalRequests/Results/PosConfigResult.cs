using System.ComponentModel.DataAnnotations;

namespace Inventory.Dto.GlobalRequests.Results
{
    public sealed class PosConfigResult
    {
        public string Currency { get; set; } = "EUR";
        public decimal DefaultVatRate { get; set; }
        public bool AllowNegativeStock { get; set; }
    }

    public sealed class GlobalConfigurationResult
    {
        public decimal DefaultVatRate { get; set; } = 21m;

        public string DefaultCurrency { get; set; } = "EUR";

        public string DefaultLocale { get; set; } = "fr-BE";

        public string DefaultTimeZone { get; set; } = "Europe/Brussels";
    }

    public sealed class UpdateGlobalConfigurationRequest
    {
        [Range(0, 100)]
        public decimal DefaultVatRate { get; set; }

        [Required]
        [MaxLength(3)]
        public string DefaultCurrency { get; set; } = "EUR";

        [Required]
        [MaxLength(20)]
        public string DefaultLocale { get; set; } = "fr-BE";

        [Required]
        [MaxLength(100)]
        public string DefaultTimeZone { get; set; } = "Europe/Brussels";
    }
}
