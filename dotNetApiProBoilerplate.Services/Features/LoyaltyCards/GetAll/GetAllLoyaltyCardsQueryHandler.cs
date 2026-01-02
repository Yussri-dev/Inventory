using Inventory.Dto.LoyaltyCards.Results;
using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.GetAll
{
    public class GetAllLoyaltyCardsQueryHandler
   : IRequestHandler<GetAllLoyaltyCardsQuery, List<LoyaltyCardResult>>
    {
        private readonly LoyaltyCardService _customerService;

        public GetAllLoyaltyCardsQueryHandler(LoyaltyCardService customerService)
        {
            _customerService = customerService;
        }

        public Task<List<LoyaltyCardResult>> Handle(GetAllLoyaltyCardsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetAllAsync();
        }
    }
}
