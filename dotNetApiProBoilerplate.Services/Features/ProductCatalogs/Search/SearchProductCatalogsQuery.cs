using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Pages.Results;
using MediatR;
using Inventory.Dto.Queries;

namespace Inventory.Services.Features.ProductCatalogs.Search
{
    public class SearchProductCatalogsQuery : IRequest<PagedResult<ProductCatalogResult>>
    {
        public ProductCatalogQuery Query { get; }

        public SearchProductCatalogsQuery(ProductCatalogQuery query)
        {
            Query = query;
        }
    }
}
