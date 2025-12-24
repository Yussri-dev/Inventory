using MediatR;

namespace Inventory.Services.Features.Customers.Delete
{
    public class DeleteCustomerCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteCustomerCommand(Guid id)
        {
            Id = id;
        }
    }
}
