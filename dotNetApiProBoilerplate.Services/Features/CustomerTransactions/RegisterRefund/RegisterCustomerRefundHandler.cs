using Inventory.Dto.CustomerTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.RegisterRefund
{
    public class RegisterCustomerRefundHandler
        : IRequestHandler<
            RegisterCustomerRefundCommand,
            CustomerTransactionResult>
    {
        private readonly CustomerTransactionService _service;

        public RegisterCustomerRefundHandler(
            CustomerTransactionService service)
        {
            _service = service;
        }

        public Task<CustomerTransactionResult> Handle(
            RegisterCustomerRefundCommand command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(command.Request);

            return _service.RegisterCustomerRefundAsync(
                command.Request,
                cancellationToken);
        }
    }
}