using Inventory.Dto.LoyaltyCards.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.LoyaltyCards.Search;
using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.Search
{
    public class SearchLoyaltyCardsQuery : IRequest<PagedResult<LoyaltyCardResult>>
    {
        public LoyaltyCardQuery Query { get; }

        public SearchLoyaltyCardsQuery(LoyaltyCardQuery query)
        {
            Query = query;
        }
    }
}
