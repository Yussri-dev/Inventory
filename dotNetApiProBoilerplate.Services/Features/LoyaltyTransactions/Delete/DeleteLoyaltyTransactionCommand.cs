using Inventory.Services.Features.LoyaltyTransactions.Delete;
using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.Delete
{
    public class DeleteLoyaltyTransactionCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteLoyaltyTransactionCommand(Guid id)
        {
            Id = id;
        }
    }
}
