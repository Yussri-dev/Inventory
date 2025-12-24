using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using MediatR;

namespace Inventory.Services.Features.Products.Search
{
    public class SearchProductsQuery : IRequest<PagedResult<ProductResult>>
    {
        public ProductQuery Query { get; }

        public SearchProductsQuery(ProductQuery query)
        {
            Query = query;
        }
    }
}
