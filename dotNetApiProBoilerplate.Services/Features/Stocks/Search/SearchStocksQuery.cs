using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Stock.Results;
using MediatR;

namespace Inventory.Services.Features.Stocks.Search
{
    public class SearchStocksQuery : IRequest<PagedResult<StockResult>>
    {
        public StockQuery Query { get; }

        public SearchStocksQuery(StockQuery query)
        {
            Query = query;
        }
    }
}
