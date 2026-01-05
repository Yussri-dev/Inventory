using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Context
{
    public interface ITenantContext
    {
        Guid GetTenantId();
        Guid GetUserId();
        string GetUserRole();
    }
}
