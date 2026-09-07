using Inventory.Dto.CustomerTransactions.Results;
using MediatR;

namespace Inventory.Services.Features
    .CustomerTransactions
    .RegisterPayment
{
    public sealed class RegisterCustomerPaymentCommandHandler
        : IRequestHandler<
            RegisterCustomerPaymentCommand,
            CustomerTransactionResult>
    {
        private readonly CustomerTransactionService _service;

        public RegisterCustomerPaymentCommandHandler(
            CustomerTransactionService service)
        {
            ArgumentNullException.ThrowIfNull(
                service);

            _service =
                service;
        }

        public Task<CustomerTransactionResult> Handle(
            RegisterCustomerPaymentCommand command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(
                command);

            ArgumentNullException.ThrowIfNull(
                command.Request);

            return _service.RegisterCustomerPaymentAsync(
                command.Request,
                cancellationToken);
        }
    }
}