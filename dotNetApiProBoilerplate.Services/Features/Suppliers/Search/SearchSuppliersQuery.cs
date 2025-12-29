using Inventory.Dto.Suppliers.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using MediatR;

namespace Inventory.Services.Features.Suppliers.Search
{
    public class SearchSuppliersQuery : IRequest<PagedResult<SupplierResult>>
    {
        public SupplierQuery Query { get; }

        public SearchSuppliersQuery(SupplierQuery query)
        {
            Query = query;
        }
    }
}
