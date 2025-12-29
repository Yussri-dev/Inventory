
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.Search
{
    public class SearchSalesQuery : IRequest<PagedResult<SaleResult>>
    {
        public SaleQuery Query { get; }

        public SearchSalesQuery(SaleQuery query)
        {
            Query = query;
        }
    }
}
