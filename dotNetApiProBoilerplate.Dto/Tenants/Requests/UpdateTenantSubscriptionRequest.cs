namespace Inventory.Dto.Tenants.Requests
{
    public sealed class UpdateTenantSubscriptionRequest
    {
        public string SubscriptionPlan { get; set; } = string.Empty;

        public DateTime? SubscriptionStartDate { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public bool IsTrialActive { get; set; }

        public DateTime? TrialEndDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsSuspended { get; set; }

        public string? SuspensionReason { get; set; }
    }
}
