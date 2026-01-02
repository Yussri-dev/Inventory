using Inventory.Dto.SupplierReturns.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.SupplierReturns.Search;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.Search
{
    public class SearchSupplierReturnsQuery : IRequest<PagedResult<SupplierReturnResult>>
    {
        public SupplierReturnQuery Query { get; }

        public SearchSupplierReturnsQuery(SupplierReturnQuery query)
        {
            Query = query;
        }
    }
}
