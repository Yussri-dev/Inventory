using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Stock.Requests;
using Inventory.Dto.Stock.Results;
using Refit;

namespace Inventory.Ui.Interfaces;

public interface IStockApi
{
    [Get("/api/v1/stocks")]
    Task<List<StockResult>> GetAll(
        CancellationToken cancellationToken = default);

    [Get("/api/v1/stocks/{id}")]
    Task<StockResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/stocks/search")]
    Task<PagedResult<StockResult>> Search(
        [Query] StockQuery query,
        CancellationToken cancellationToken = default);

    [Put("/api/v1/stocks/{id}")]
    Task<StockResult> Update(
        Guid id,
        [Body] UpdateStockRequest request,
        CancellationToken cancellationToken = default);
}