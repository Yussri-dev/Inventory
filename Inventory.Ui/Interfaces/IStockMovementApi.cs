using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.StockMouvements.Requests;
using Inventory.Dto.StockMouvements.Results;
using Refit;

namespace Inventory.Ui.Interfaces;

public interface IStockMovementApi
{
    [Post("/api/v1/stockMouvements")]
    Task<StockMouvementResult> Create(
        [Body] CreateStockMouvementRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/stockMouvements/search")]
    Task<PagedResult<StockMouvementResult>> Search(
        [Query] StockMouvementQuery query,
        CancellationToken cancellationToken = default);
}