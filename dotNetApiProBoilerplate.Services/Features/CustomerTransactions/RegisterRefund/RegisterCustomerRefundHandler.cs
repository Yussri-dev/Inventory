using Inventory.Dto.CustomerTransactions.Results;
using MediatR;


namespace Inventory.Services.Features.CustomerTransactions.RegisterRefund
{
    // Handler
    public class RegisterCustomerRefundHandler
        : IRequestHandler<RegisterCustomerRefundCommand, CustomerTransactionResult>
    {
        private readonly CustomerTransactionService _service;

        public RegisterCustomerRefundHandler(CustomerTransactionService service)
        {
            _service = service;
        }

        public async Task<CustomerTransactionResult> Handle(
            RegisterCustomerRefundCommand request,
            CancellationToken cancellationToken)
        {
            return await _service.RegisterCustomerRefundAsync(
                request.Request.CustomerId,
                request.Request.Amount,
                request.Request.Description,
                request.Request.IsCash
            );
        }
    }
}
