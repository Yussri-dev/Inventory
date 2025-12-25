using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.Delete
{
    public class DeleteCustomerTransactionCommandHandler
         : IRequestHandler<DeleteCustomerTransactionCommand, Unit>
    {
        private readonly CustomerTransactionService _customerService;

        public DeleteCustomerTransactionCommandHandler(CustomerTransactionService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteCustomerTransactionCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
