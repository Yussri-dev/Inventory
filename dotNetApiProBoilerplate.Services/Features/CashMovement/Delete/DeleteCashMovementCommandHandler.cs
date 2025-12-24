using MediatR;

namespace Inventory.Services.Features.CashMovement.Delete
{
    public class DeleteCashMovementCommandHandler
         : IRequestHandler<DeleteCashMovementCommand, Unit>
    {
        private readonly CashMovementService _customerService;

        public DeleteCashMovementCommandHandler(CashMovementService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteCashMovementCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
