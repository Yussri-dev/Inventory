using Inventory.Dto.LoyaltyCards.Results;
using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.GetById
{
    public class GetLoyaltyCardByIdQueryHandler
       : IRequestHandler<GetLoyaltyCardByIdQuery, LoyaltyCardResult>
    {
        private readonly LoyaltyCardService _customerService;

        public GetLoyaltyCardByIdQueryHandler(LoyaltyCardService customerService)
        {
            _customerService = customerService;
        }

        public Task<LoyaltyCardResult> Handle(GetLoyaltyCardByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
