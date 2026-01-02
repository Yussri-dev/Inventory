using Inventory.Dto.Promotions.Results;
using MediatR;

namespace Inventory.Services.Features.Promotions.Update
{
    public class UpdatePromotionCommandHandler
       : IRequestHandler<UpdatePromotionCommand, PromotionResult>
    {
        private readonly PromotionService _customerService;

        public UpdatePromotionCommandHandler(PromotionService customerService)
        {
            _customerService = customerService;
        }

        public Task<PromotionResult> Handle(UpdatePromotionCommand command, CancellationToken cancellationToken)
        {
            return _customerService.UpdateAsync(command.Id, command.Request);
        }
    }
}
