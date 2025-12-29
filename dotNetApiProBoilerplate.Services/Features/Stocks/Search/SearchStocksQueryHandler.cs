using Inventory.Dto.Pages.Results;
using Inventory.Dto.Stock.Results;
using MediatR;

namespace Inventory.Services.Features.Stocks.Search
{
    public class SearchStocksQueryHandler
    : IRequestHandler<SearchStocksQuery, PagedResult<StockResult>>
    {
        private readonly StockService _productService;

        public SearchStocksQueryHandler(StockService productService)
        {
            _productService = productService;
        }

        public Task<PagedResult<StockResult>> Handle(SearchStocksQuery query, CancellationToken cancellationToken)
        {
            return _productService.QueryAsync(query.Query);
        }
    }
}
