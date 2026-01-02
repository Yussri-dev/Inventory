using Inventory.Dto.LoyaltyCards.Results;
using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.Create
{
    public class CreateLoyaltyCardCommandHandler : IRequestHandler<CreateLoyaltyCardCommand, LoyaltyCardResult>
    {
        private readonly LoyaltyCardService _customerService;

        public CreateLoyaltyCardCommandHandler(LoyaltyCardService productService)
        {
            _customerService = productService;
        }

        public Task<LoyaltyCardResult> Handle(CreateLoyaltyCardCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }
}
