using Inventory.Dto.Pages.Results;
using Inventory.Dto.Purchases.Results;
using MediatR;

namespace Inventory.Services.Features.Purchases.Search
{
    public class SearchPurchasesQueryHandler
    : IRequestHandler<SearchPurchasesQuery, PagedResult<PurchaseResult>>
    {
        private readonly PurchaseService _productService;

        public SearchPurchasesQueryHandler(PurchaseService productService)
        {
            _productService = productService;
        }

        public Task<PagedResult<PurchaseResult>> Handle(SearchPurchasesQuery query, CancellationToken cancellationToken)
        {
            return _productService.QueryAsync(query.Query);
        }
    }
}
