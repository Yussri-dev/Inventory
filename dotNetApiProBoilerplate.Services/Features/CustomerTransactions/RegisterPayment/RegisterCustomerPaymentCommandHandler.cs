using Inventory.Dto.CustomerTransactions.Results;
using MediatR;


namespace Inventory.Services.Features.CustomerTransactions.RegisterPayment
{
    public class RegisterCustomerPaymentCommandHandler
       : IRequestHandler<RegisterCustomerPaymentCommand, CustomerTransactionResult>
    {
        private readonly CustomerTransactionService _service;

        public RegisterCustomerPaymentCommandHandler(CustomerTransactionService service)
        {
            _service = service;
        }

        public async Task<CustomerTransactionResult> Handle(
            RegisterCustomerPaymentCommand request,
            CancellationToken cancellationToken)
        {
            return await _service.RegisterCustomerPaymentAsync(
                request.Request.CustomerId,
                request.Request.Amount,
                request.Request.Description,
                request.Request.IsCash
            );
        }
    }
}
