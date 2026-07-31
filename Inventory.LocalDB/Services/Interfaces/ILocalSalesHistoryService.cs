
using Inventory.LocalDB.Services.Results;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalSalesHistoryService
    {
        Task<LocalSalesHistoryPageResult> SearchAsync(
            LocalSalesHistoryQuery query,
            CancellationToken cancellationToken = default);

        Task<LocalSalesHistoryDetailsResult?> GetByIdAsync(
            Guid localSaleId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<LocalSalesHistoryCustomerResult>>
            SearchCustomersAsync(
                string? search,
                int maximumResults = 20,
                CancellationToken cancellationToken = default);
    }
}
