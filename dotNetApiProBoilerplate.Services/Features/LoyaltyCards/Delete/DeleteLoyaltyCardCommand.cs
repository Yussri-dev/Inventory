using Inventory.Services.Features.LoyaltyCards.Delete;
using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.Delete
{
    public class DeleteLoyaltyCardCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteLoyaltyCardCommand(Guid id)
        {
            Id = id;
        }
    }
}
