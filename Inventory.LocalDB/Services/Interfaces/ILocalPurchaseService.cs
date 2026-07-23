using Inventory.LocalDB.Services.Requests;
using Inventory.LocalDB.Services.Results;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalPurchaseService
    {
        Task<LocalPurchaseResult> CreateCompleteAsync(
            CreateLocalPurchaseRequest request,
            CancellationToken cancellationToken = default);

        Task<LocalPurchaseResult> GetByIdAsync(
            Guid localPurchaseId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LocalPurchaseResult>> GetRecentAsync(
            int take = 50,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LocalPurchaseResult>> GetPendingAsync(
            CancellationToken cancellationToken = default);
    }
}
