namespace Inventory.Domain.Models
{
    public sealed class GlobalSetting
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public decimal DefaultVatRate { get; set; } = 21m;

        public string DefaultCurrency { get; set; } = "EUR";

        public string DefaultLocale { get; set; } = "fr-BE";

        public string DefaultTimeZone { get; set; } =
            "Europe/Brussels";

        public DateTime CreatedAtUtc { get; set; } =
            DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public Guid? UpdatedByUserId { get; set; }
    }
}
