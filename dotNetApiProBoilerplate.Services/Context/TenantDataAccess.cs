using Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace Inventory.Services.Context;

// Unlike TenantContext, construction must also work during login/registration.
// Unauthenticated access gets no business-data access by default.
public sealed class TenantDataAccess : ITenantDataAccess
{
    public bool IsAuthenticated { get; }
    public bool IsSuperAdmin { get; }
    public Guid TenantId { get; }

    public TenantDataAccess(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        IsAuthenticated = user?.Identity?.IsAuthenticated == true;
        IsSuperAdmin = IsAuthenticated && user!.IsInRole("SuperAdmin");
        if (IsAuthenticated && Guid.TryParse(user!.FindFirst("TenantId")?.Value, out var tenantId))
            TenantId = tenantId;
    }
}