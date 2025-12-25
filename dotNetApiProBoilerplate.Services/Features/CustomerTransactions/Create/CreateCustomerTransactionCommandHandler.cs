using Inventory.Dto.CustomerTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.Create
{
    public class CreateCustomerTransactionCommandHandler : IRequestHandler<CreateCustomerTransactionCommand, CustomerTransactionResult>
    {
        private readonly CustomerTransactionService _cashCorrectionService;

        public CreateCustomerTransactionCommandHandler(CustomerTransactionService productService)
        {
            _cashCorrectionService = productService;
        }

        public Task<CustomerTransactionResult> Handle(CreateCustomerTransactionCommand command, CancellationToken cancellationToken)
        {
            return _cashCorrectionService.CreateAsync(command.Request);
        }
    }
}
