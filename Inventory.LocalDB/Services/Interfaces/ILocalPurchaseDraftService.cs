using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Requests;


namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalPurchaseDraftService
    {
        Task<LocalPurchaseDraft?> GetActiveAsync();

        Task<Guid> SaveAsync(SaveLocalPurchaseDraftRequest request);

        Task SuspendAsync(Guid draftId);

        Task DeleteAsync(Guid draftId);
    }
}
