using Inventory.Dto.LoyaltyCards.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.Search
{
    public class SearchLoyaltyCardsQueryHandler
    : IRequestHandler<SearchLoyaltyCardsQuery, PagedResult<LoyaltyCardResult>>
    {
        private readonly LoyaltyCardService _customerService;

        public SearchLoyaltyCardsQueryHandler(LoyaltyCardService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<LoyaltyCardResult>> Handle(SearchLoyaltyCardsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
