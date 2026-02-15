using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Stock.Results;
using Inventory.Dto.StockMouvements.Requests;
using Inventory.Dto.StockMouvements.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IStockMovementApi
    {
        [Post("/api/v1/stockMouvements")]
        Task<StockMouvementResult> Create([Body] CreateStockMouvementRequest request);

        [Get("/api/v1/stockMouvements/search")]
        Task<PagedResult<StockMouvementResult>> Search([Query] StockMouvementQuery query);

        [Get("/api/v1/stocks")]
        Task<List<StockResult>> GetAll();

        [Get("/api/v1/stocks/search")]
        Task<PagedResult<StockResult>> Query([Query] StockQuery query);
    }
}
