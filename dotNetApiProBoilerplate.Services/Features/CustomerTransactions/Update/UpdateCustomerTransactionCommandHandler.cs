using Inventory.Dto.CustomerTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.Update
{
    public class UpdateCustomerTransactionCommandHandler
       : IRequestHandler<UpdateCustomerTransactionCommand, CustomerTransactionResult>
    {
        private readonly CustomerTransactionService _cashCorrectionService;

        public UpdateCustomerTransactionCommandHandler(CustomerTransactionService customerService)
        {
            _cashCorrectionService = customerService;
        }

        public Task<CustomerTransactionResult> Handle(UpdateCustomerTransactionCommand command, CancellationToken cancellationToken)
        {
            return _cashCorrectionService.UpdateAsync(command.Id, command.Request);
        }
    }
}
