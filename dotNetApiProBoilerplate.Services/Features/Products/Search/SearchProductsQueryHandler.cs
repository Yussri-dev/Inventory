using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Results;
using MediatR;


namespace Inventory.Services.Features.Products.Search
{
    public class SearchProductsQueryHandler
    : IRequestHandler<SearchProductsQuery, PagedResult<ProductResult>>
    {
        private readonly ProductService _productService;

        public SearchProductsQueryHandler(ProductService productService)
        {
            _productService = productService;
        }

        public Task<PagedResult<ProductResult>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
        {
            return _productService.QueryAsync(query.Query);
        }
    }
}
