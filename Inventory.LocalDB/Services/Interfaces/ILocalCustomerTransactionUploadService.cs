using Inventory.LocalDB.Services.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalCustomerTransactionUploadService
    {
        Task<LocalCustomerTransactionUploadResult>
            UploadPendingAsync(
                CancellationToken cancellationToken = default);
    }
}
