using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Tenants.Results
{
    // DTOs
    public sealed class TenantResponse
    {
        public Guid Id { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public string? LegalName { get; set; }

        public string? TradeName { get; set; }

        public string? TaxNumber { get; set; }

        public string? RegistrationNumber { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? PostalCode { get; set; }

        public string? Country { get; set; }

        public string? Phone { get; set; }

        public string? Mobile { get; set; }

        public string? Email { get; set; }

        public string? Website { get; set; }

        public string? LogoUrl { get; set; }

        public string? ReceiptHeader { get; set; }

        public string? ReceiptFooter { get; set; }

        public string Currency { get; set; } =
            "EUR";

        public string? CurrencySymbol { get; set; }

        public string Locale { get; set; } =
            "fr-BE";

        public string TimeZone { get; set; } =
            "Europe/Brussels";

        public string DateFormat { get; set; } =
            "dd/MM/yyyy";

        public string TimeFormat { get; set; } =
            "HH:mm";

        public decimal DefaultVatRate { get; set; }

        public string SubscriptionPlan { get; set; } = string.Empty;

        public DateTime? SubscriptionStartDate { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public bool IsTrialActive { get; set; }

        public DateTime? TrialEndDate { get; set; }

        public int MaxUsers { get; set; }

        public int MaxProducts { get; set; }

        public int MaxLocations { get; set; }

        public int MaxMonthlyTransactions { get; set; }

        public int CurrentUsers { get; set; }

        public int CurrentProducts { get; set; }

        public int CurrentLocations { get; set; }

        public int CurrentMonthTransactions { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastActivityAt { get; set; }
    }
}
