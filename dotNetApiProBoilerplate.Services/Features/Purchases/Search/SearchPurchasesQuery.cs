using Inventory.Dto.Pages.Results;
using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.Purchases.Search;
using MediatR;

namespace Inventory.Services.Features.Purchases.Search
{
    public class SearchPurchasesQuery : IRequest<PagedResult<PurchaseResult>>
    {
        public PurchaseQuery Query { get; }

        public SearchPurchasesQuery(PurchaseQuery query)
        {
            Query = query;
        }
    }
}
