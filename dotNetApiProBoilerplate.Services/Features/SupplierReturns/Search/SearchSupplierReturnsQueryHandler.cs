using Inventory.Dto.SupplierReturns.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.Search
{
    public class SearchSupplierReturnsQueryHandler
    : IRequestHandler<SearchSupplierReturnsQuery, PagedResult<SupplierReturnResult>>
    {
        private readonly SupplierReturnService _customerService;

        public SearchSupplierReturnsQueryHandler(SupplierReturnService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<SupplierReturnResult>> Handle(SearchSupplierReturnsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
