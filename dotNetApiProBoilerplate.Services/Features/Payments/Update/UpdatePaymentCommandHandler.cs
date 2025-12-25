using Inventory.Dto.Payments.Results;
using MediatR;

namespace Inventory.Services.Features.Payments.Update
{
    public class UpdatePaymentCommandHandler
       : IRequestHandler<UpdatePaymentCommand, PaymentResult>
    {
        private readonly PaymentService _customerService;

        public UpdatePaymentCommandHandler(PaymentService customerService)
        {
            _customerService = customerService;
        }

        public Task<PaymentResult> Handle(UpdatePaymentCommand command, CancellationToken cancellationToken)
        {
            return _customerService.UpdateAsync(command.Id, command.Request);
        }
    }
}
