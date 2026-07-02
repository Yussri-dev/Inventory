using Inventory.Dto.Enums;

namespace Inventory.Services
{
    public sealed record SubscriptionLimits(
        int MaxUsers,
        int MaxProducts,
        int MaxLocations,
        int MaxMonthlyTransactions);

    public static class SubscriptionPlanResolver
    {
        public static SubscriptionLimits GetLimits(
            SubscriptionPlan plan)
        {
            return plan switch
            {
                SubscriptionPlan.Free =>
                    new SubscriptionLimits(
                        MaxUsers: 5,
                        MaxProducts: 1000,
                        MaxLocations: 1,
                        MaxMonthlyTransactions: 10000),

                SubscriptionPlan.Basic =>
                    new SubscriptionLimits(
                        MaxUsers: 10,
                        MaxProducts: 5000,
                        MaxLocations: 3,
                        MaxMonthlyTransactions: 50000),

                SubscriptionPlan.Professional =>
                    new SubscriptionLimits(
                        MaxUsers: 50,
                        MaxProducts: 50000,
                        MaxLocations: 10,
                        MaxMonthlyTransactions: 250000),

                SubscriptionPlan.Enterprise =>
                    new SubscriptionLimits(
                        MaxUsers: 500,
                        MaxProducts: 500000,
                        MaxLocations: 100,
                        MaxMonthlyTransactions: 2000000),

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(plan),
                        plan,
                        "Unsupported subscription plan.")
            };
        }
    }
}
