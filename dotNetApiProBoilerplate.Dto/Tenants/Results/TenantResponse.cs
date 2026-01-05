using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Dto.Tenants.Results
{
    // DTOs
    public class TenantResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? LegalName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? TaxNumber { get; set; }
        public string Currency { get; set; } = null!;
        public string? CurrencySymbol { get; set; }
        public string Locale { get; set; } = null!;
        public string TimeZone { get; set; } = null!;
        public string DateFormat { get; set; } = null!;
        public string TimeFormat { get; set; } = null!;
        public decimal DefaultVatRate { get; set; }
        public string SubscriptionPlan { get; set; } = null!;
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
