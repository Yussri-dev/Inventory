using Inventory.Dto.Customers.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using MediatR;

namespace Inventory.Services.Features.Customers.Search
{
    public class SearchCustomersQuery : IRequest<PagedResult<CustomerResult>>
    {
        public CustomerQuery Query { get; }

        public SearchCustomersQuery(CustomerQuery query)
        {
            Query = query;
        }
    }
}
