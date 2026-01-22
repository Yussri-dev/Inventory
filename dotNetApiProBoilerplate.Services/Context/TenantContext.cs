using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Inventory.Services.Context
{
    public sealed class TenantContext : ITenantContext
    {
        public Guid UserId { get; }
        public Guid TenantId { get; }
        public bool IsSuperAdmin { get; }
        public bool IsAdmin { get; }

        public TenantContext(IHttpContextAccessor accessor)
        {
            var user = accessor.HttpContext?.User
                ?? throw new UnauthorizedAccessException();

            UserId = Guid.Parse(
                user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("UserId missing")
            );

            IsSuperAdmin = user.IsInRole("SuperAdmin");
            IsAdmin = user.IsInRole("Admin") || IsSuperAdmin;

            var tenantClaim = user.FindFirstValue("TenantId");

            if (!IsSuperAdmin && string.IsNullOrEmpty(tenantClaim))
                throw new UnauthorizedAccessException("TenantId missing");

            TenantId = string.IsNullOrEmpty(tenantClaim)
                ? Guid.Empty
                : Guid.Parse(tenantClaim);
        }
    }

}
