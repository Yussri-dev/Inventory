using Inventory.Dto.Pages.Results;
using Inventory.Dto.PurchaseLines.Results;
using MediatR;

namespace Inventory.Services.Features.PurchaseLines.Search
{
    public class SearchPurchaseLinesQueryHandler
    : IRequestHandler<SearchPurchaseLinesQuery, PagedResult<PurchaseLineResult>>
    {
        private readonly PurchaseLineService _productService;

        public SearchPurchaseLinesQueryHandler(PurchaseLineService productService)
        {
            _productService = productService;
        }

        public Task<PagedResult<PurchaseLineResult>> Handle(SearchPurchaseLinesQuery query, CancellationToken cancellationToken)
        {
            return _productService.QueryAsync(query.Query);
        }
    }
}
