using Inventory.LocalDB.Models;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalStockAdjustmentService
    {
        Task<IReadOnlyList<LocalStock>> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<int> CountAsync(
            string? search,
            CancellationToken cancellationToken = default);

        Task<LocalStock> AdjustAsync(
            Guid localStockId,
            decimal newQuantity,
            string? notes,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LocalStockMovement>> GetHistoryAsync(
            Guid productLocalId,
            int maximumResults = 100,
            CancellationToken cancellationToken = default);
    }
}
