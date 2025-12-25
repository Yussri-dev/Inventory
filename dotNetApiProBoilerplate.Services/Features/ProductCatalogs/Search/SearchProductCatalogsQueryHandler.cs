using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCatalogs.Search
{
    public class SearchProductCatalogsQueryHandler
    : IRequestHandler<SearchProductCatalogsQuery, PagedResult<ProductCatalogResult>>
    {
        private readonly ProductCatalogService _customerService;

        public SearchProductCatalogsQueryHandler(ProductCatalogService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<ProductCatalogResult>> Handle(SearchProductCatalogsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
