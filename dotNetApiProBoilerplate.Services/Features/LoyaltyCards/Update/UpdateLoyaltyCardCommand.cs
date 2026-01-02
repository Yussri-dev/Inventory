using Inventory.Dto.LoyaltyCards.Results;
using Inventory.Dto.LoyaltyCards.Requests;
using Inventory.Services.Features.LoyaltyCards.Update;
using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.Update
{
    public class UpdateLoyaltyCardCommand : IRequest<LoyaltyCardResult>
    {
        public Guid Id { get; }
        public UpdateLoyaltyCardRequest Request { get; }

        public UpdateLoyaltyCardCommand(Guid id, UpdateLoyaltyCardRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
