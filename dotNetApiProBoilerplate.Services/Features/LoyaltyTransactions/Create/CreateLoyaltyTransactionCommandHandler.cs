using Inventory.Dto.LoyaltyTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.Create
{
    public class CreateLoyaltyTransactionCommandHandler : IRequestHandler<CreateLoyaltyTransactionCommand, LoyaltyTransactionResult>
    {
        private readonly LoyaltyTransactionService _customerService;

        public CreateLoyaltyTransactionCommandHandler(LoyaltyTransactionService productService)
        {
            _customerService = productService;
        }

        public Task<LoyaltyTransactionResult> Handle(CreateLoyaltyTransactionCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }
}
