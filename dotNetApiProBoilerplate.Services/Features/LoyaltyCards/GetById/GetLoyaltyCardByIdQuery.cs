using Inventory.Dto.LoyaltyCards.Results;
using Inventory.Services.Features.LoyaltyCards.GetById;
using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.GetById
{
    public class GetLoyaltyCardByIdQuery : IRequest<LoyaltyCardResult>
    {
        public Guid Id { get; }

        public GetLoyaltyCardByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
