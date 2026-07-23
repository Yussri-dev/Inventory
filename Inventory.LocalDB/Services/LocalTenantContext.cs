using Inventory.LocalDB.Services.Interfaces;

namespace Inventory.LocalDB.Services
{
    public sealed class LocalTenantContext : ILocalTenantContext
    {
        private readonly object _lock = new();

        private Guid? _tenantId;

        public Guid? TenantId
        {
            get
            {
                lock (_lock)
                {
                    return _tenantId;
                }
            }
        }

        public bool HasTenant
        {
            get
            {
                lock (_lock)
                {
                    return _tenantId.HasValue && _tenantId.Value != Guid.Empty;
                }
            }
        }

        public void SetTenant(Guid tenantId)
        {
            if (tenantId == Guid.Empty)
            {
                throw new ArgumentException("Tenant ID cannot be empty.", nameof(tenantId));
            }

            lock (_lock)
            {
                _tenantId = tenantId;
            }
        }

        public Guid GetRequiredTenantId()
        {
            lock (_lock)
            {
                if (!_tenantId.HasValue ||
                    _tenantId.Value == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        "No tenant is currently loaded in the local session.");
                }

                return _tenantId.Value;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _tenantId = null;
            }
        }
        
    }
}
