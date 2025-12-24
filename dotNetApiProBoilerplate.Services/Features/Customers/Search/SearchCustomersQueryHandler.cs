using Inventory.Dto.Customers.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.Customers.Search
{
    public class SearchCustomersQueryHandler
    : IRequestHandler<SearchCustomersQuery, PagedResult<CustomerResult>>
    {
        private readonly CustomerService _customerService;

        public SearchCustomersQueryHandler(CustomerService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<CustomerResult>> Handle(SearchCustomersQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
