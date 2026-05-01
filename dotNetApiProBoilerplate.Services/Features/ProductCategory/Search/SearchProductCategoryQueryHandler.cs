using Inventory.Dto.Pages.Results;
using Inventory.Dto.ProductCategory.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCategory.Search
{
    public class SearchProductCategoryQueryHandler
        : IRequestHandler<SearchProductCategoryQuery, PagedResult<ProductCategoryResult>>
    {
        private readonly ProductCategoryService _productCategoryService;
        public SearchProductCategoryQueryHandler(ProductCategoryService productCategoryService)
        {
            _productCategoryService = productCategoryService;
        }
        public Task<PagedResult<ProductCategoryResult>> Handle(SearchProductCategoryQuery query, CancellationToken cancellationToken)
        {
            return _productCategoryService.QueryAsync(query.Query);
        }
    }
}
