
namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalTenantContext
    {
        Guid? TenantId { get; }

        bool HasTenant { get; }

        void SetTenant(Guid tenantId);

        Guid GetRequiredTenantId();

        void Clear();
    }
}
