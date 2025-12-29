
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.Search
{
    public class SearchSalesQueryHandler
    : IRequestHandler<SearchSalesQuery, PagedResult<SaleResult>>
    {
        private readonly SaleService _productService;

        public SearchSalesQueryHandler(SaleService productService)
        {
            _productService = productService;
        }

        public Task<PagedResult<SaleResult>> Handle(SearchSalesQuery query, CancellationToken cancellationToken)
        {
            return _productService.QueryAsync(query.Query);
        }
    }
}
