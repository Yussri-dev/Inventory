using Inventory.Dto.Suppliers.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.Suppliers.Search
{
    public class SearchSuppliersQueryHandler
    : IRequestHandler<SearchSuppliersQuery, PagedResult<SupplierResult>>
    {
        private readonly SupplierService _customerService;

        public SearchSuppliersQueryHandler(SupplierService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<SupplierResult>> Handle(SearchSuppliersQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
