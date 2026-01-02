using Inventory.Dto.Promotions.Results;
using MediatR;

namespace Inventory.Services.Features.Promotions.Create
{
    public class CreatePromotionCommandHandler : IRequestHandler<CreatePromotionCommand, PromotionResult>
    {
        private readonly PromotionService _customerService;

        public CreatePromotionCommandHandler(PromotionService productService)
        {
            _customerService = productService;
        }

        public Task<PromotionResult> Handle(CreatePromotionCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }
}
