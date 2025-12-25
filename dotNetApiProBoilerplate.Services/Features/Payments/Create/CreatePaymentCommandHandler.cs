using Inventory.Dto.Payments.Results;
using MediatR;

namespace Inventory.Services.Features.Payments.Create
{
    public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentResult>
    {
        private readonly PaymentService _customerService;

        public CreatePaymentCommandHandler(PaymentService productService)
        {
            _customerService = productService;
        }

        public Task<PaymentResult> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }
}
