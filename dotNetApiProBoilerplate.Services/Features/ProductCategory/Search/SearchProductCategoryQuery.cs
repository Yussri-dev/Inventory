using Inventory.Dto.Pages.Results;
using Inventory.Dto.ProductCategory.Results;
using Inventory.Dto.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.ProductCategory.Search
{
    public class SearchProductCategoryQuery : IRequest<PagedResult<ProductCategoryResult>>
    {
        public ProductCategoryQuery Query { get; }

        public SearchProductCategoryQuery(ProductCategoryQuery query)
        {
            Query = query;
        }
    }
}
