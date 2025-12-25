using MediatR;

namespace Inventory.Services.Features.CashSession.Delete
{
    public class DeleteCashSessionCommandHandler
         : IRequestHandler<DeleteCashSessionCommand, Unit>
    {
        private readonly CashSessionService _customerService;

        public DeleteCashSessionCommandHandler(CashSessionService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteCashSessionCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
