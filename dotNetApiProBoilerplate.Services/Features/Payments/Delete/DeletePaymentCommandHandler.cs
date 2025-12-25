using MediatR;

namespace Inventory.Services.Features.Payments.Delete
{
    public class DeletePaymentCommandHandler
         : IRequestHandler<DeletePaymentCommand, Unit>
    {
        private readonly PaymentService _customerService;

        public DeletePaymentCommandHandler(PaymentService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeletePaymentCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
