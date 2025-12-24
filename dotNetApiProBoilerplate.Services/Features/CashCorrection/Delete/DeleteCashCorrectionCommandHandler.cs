using MediatR;

namespace Inventory.Services.Features.CashCorrection.Delete
{
    public class DeleteCashCorrectionCommandHandler
         : IRequestHandler<DeleteCashCorrectionCommand, Unit>
    {
        private readonly CashCorrectionService _customerService;

        public DeleteCashCorrectionCommandHandler(CashCorrectionService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteCashCorrectionCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
