using Inventory.Dto.Pages.Results;
using Inventory.Dto.SaleLines.Results;
using MediatR;

namespace Inventory.Services.Features.SaleLines.Search
{
    public class SearchSaleLinesQueryHandler
    : IRequestHandler<SearchSaleLinesQuery, PagedResult<SaleLineResult>>
    {
        private readonly SaleLineService _productService;

        public SearchSaleLinesQueryHandler(SaleLineService productService)
        {
            _productService = productService;
        }

        public Task<PagedResult<SaleLineResult>> Handle(SearchSaleLinesQuery query, CancellationToken cancellationToken)
        {
            return _productService.QueryAsync(query.Query);
        }
    }
}
