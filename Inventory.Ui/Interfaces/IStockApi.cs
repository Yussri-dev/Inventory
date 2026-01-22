using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.ReturnLines.Requests;
using Inventory.Dto.ReturnLines.Results;
using Inventory.Dto.Returns.Requests;
using Inventory.Dto.Returns.Results;
using Inventory.Dto.Sales.Results;
using Inventory.Dto.Stock.Requests;
using Inventory.Dto.Stock.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IStockApi
    {
        [Get("/api/v1/stocks")]
        Task<List<StockResult>> GetAll();

        [Get("/api/v1/stocks/{id}")]
        Task<StockResult> GetById(Guid id);

        [Get("/api/v1/stocks/search")]
        Task<PagedResult<StockResult>> Search([Query] StockQuery query);

        [Put("/api/v1/stocks/{id}")]
        Task<StockResult> Update(Guid id, [Body] UpdateStockRequest request);
    }
}
