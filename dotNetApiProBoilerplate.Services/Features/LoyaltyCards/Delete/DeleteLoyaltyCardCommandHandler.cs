using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.Delete
{
    public class DeleteLoyaltyCardCommandHandler
         : IRequestHandler<DeleteLoyaltyCardCommand, Unit>
    {
        private readonly LoyaltyCardService _customerService;

        public DeleteLoyaltyCardCommandHandler(LoyaltyCardService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteLoyaltyCardCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
