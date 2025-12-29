using Inventory.Dto.Pages.Results;
using Inventory.Dto.PurchaseLines.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.PurchaseLines.Search;
using MediatR;

namespace Inventory.Services.Features.PurchaseLines.Search
{
    public class SearchPurchaseLinesQuery : IRequest<PagedResult<PurchaseLineResult>>
    {
        public PurchaseLineQuery Query { get; }

        public SearchPurchaseLinesQuery(PurchaseLineQuery query)
        {
            Query = query;
        }
    }
}
