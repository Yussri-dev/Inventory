using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Requests;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalStoreProfileService
    {
        Task UpsertAsync(
            LocalStoreProfile profile,
            CancellationToken cancellationToken = default);

        Task<LocalStoreProfile?> GetCurrentAsync(
            CancellationToken cancellationToken = default);

        Task<LocalStoreProfile> UpdateReceiptConfigurationAsync(
            UpdateLocalReceiptConfigurationRequest request,
            CancellationToken cancellationToken = default);
    }
}
