using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Inventory.Services.Context
{
    public class TenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid GetTenantId()
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue("TenantId");

            if (string.IsNullOrEmpty(tenantIdClaim))
            {
                throw new UnauthorizedAccessException("TenantId not found in token");
            }

            return Guid.Parse(tenantIdClaim);
        }

        public Guid GetUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("UserId not found in token");
            }

            return Guid.Parse(userIdClaim);
        }

        public string GetUserRole()
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.Role);

            return roleClaim ?? "User";
        }
    }
}
