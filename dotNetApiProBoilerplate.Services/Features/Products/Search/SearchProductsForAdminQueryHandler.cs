using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Results;
using MediatR;


namespace Inventory.Services.Features.Products.Search
{
    public sealed class SearchProductsForAdminQueryHandler
       : IRequestHandler<SearchProductsForAdminQuery, PagedResult<ProductResult>>
    {
        private readonly ProductService _productService;

        public SearchProductsForAdminQueryHandler(
            ProductService productService)
        {
            _productService = productService;
        }

        public async Task<PagedResult<ProductResult>> Handle(
            SearchProductsForAdminQuery request,
            CancellationToken cancellationToken)
        {
            return await _productService.QueryForAdminAsync(
                request.Query);
        }
    }
}
