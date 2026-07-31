using Inventory.Dto.Tenants.Results;
using Refit;
namespace Inventory.Ui.Interfaces
{
    public interface ITenantApi
    {
        [Get("/api/Tenant/me")]
        Task<TenantResponse> GetMyTenantAsync(
            CancellationToken cancellationToken = default);
    }
}
