using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.Delete
{
    public class DeleteLoyaltyTransactionCommandHandler
         : IRequestHandler<DeleteLoyaltyTransactionCommand, Unit>
    {
        private readonly LoyaltyTransactionService _customerService;

        public DeleteLoyaltyTransactionCommandHandler(LoyaltyTransactionService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteLoyaltyTransactionCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
