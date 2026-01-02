using Inventory.Dto.LoyaltyCards.Results;
using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.Update
{
    public class UpdateLoyaltyCardCommandHandler
      : IRequestHandler<UpdateLoyaltyCardCommand, LoyaltyCardResult>
    {
        private readonly LoyaltyCardService _customerService;

        public UpdateLoyaltyCardCommandHandler(LoyaltyCardService customerService)
        {
            _customerService = customerService;
        }

        public Task<LoyaltyCardResult> Handle(UpdateLoyaltyCardCommand command, CancellationToken cancellationToken)
        {
            return _customerService.UpdateAsync(command.Id, command.Request);
        }
    }
}
