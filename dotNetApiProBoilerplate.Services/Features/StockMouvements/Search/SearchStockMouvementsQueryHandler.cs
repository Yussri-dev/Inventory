using Inventory.Dto.Pages.Results;
using Inventory.Dto.StockMouvements.Results;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.Search
{
    public class SearchStockMouvementsQueryHandler
    : IRequestHandler<SearchStockMouvementsQuery, PagedResult<StockMouvementResult>>
    {
        private readonly StockMouvementService _productService;

        public SearchStockMouvementsQueryHandler(StockMouvementService productService)
        {
            _productService = productService;
        }

        public Task<PagedResult<StockMouvementResult>> Handle(SearchStockMouvementsQuery query, CancellationToken cancellationToken)
        {
            return _productService.QueryAsync(query.Query);
        }
    }
}
