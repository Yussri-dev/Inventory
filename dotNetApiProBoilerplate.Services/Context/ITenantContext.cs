

namespace Inventory.Services.Context
{
    public interface ITenantContext
    {
        Guid UserId { get; }
        Guid TenantId { get; }
        bool IsSuperAdmin { get; }
        bool IsAdmin { get; }
        bool IsCashier { get; }

    }

}
