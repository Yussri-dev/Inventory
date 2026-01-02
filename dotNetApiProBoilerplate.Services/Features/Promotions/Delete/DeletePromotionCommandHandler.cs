using MediatR;

namespace Inventory.Services.Features.Promotions.Delete
{
    public class DeletePromotionCommandHandler
         : IRequestHandler<DeletePromotionCommand, Unit>
    {
        private readonly PromotionService _customerService;

        public DeletePromotionCommandHandler(PromotionService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeletePromotionCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
