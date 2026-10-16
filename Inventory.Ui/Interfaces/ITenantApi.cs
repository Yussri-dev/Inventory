using Inventory.Dto.Tenants.Requests;
using Inventory.Dto.Tenants.Results;
using Refit;
namespace Inventory.Ui.Interfaces
{
    public interface ITenantApi
    {
        [Get("/api/Tenant/me")]
        Task<TenantResponse> GetMyTenantAsync(
            CancellationToken cancellationToken = default);
        [Get("/api/Tenant/subscriptions")]
        Task<List<TenantResponse>> GetSubscriptionsAsync();

        [Put("/api/Tenant/{tenantId}/subscription")]
        Task<TenantResponse> UpdateSubscriptionAsync(
            Guid tenantId,
            [Body] UpdateTenantSubscriptionRequest request);
    }
}
