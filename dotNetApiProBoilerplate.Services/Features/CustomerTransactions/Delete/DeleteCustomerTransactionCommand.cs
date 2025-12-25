using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.Delete
{
    public class DeleteCustomerTransactionCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteCustomerTransactionCommand(Guid id)
        {
            Id = id;
        }
    }
}
