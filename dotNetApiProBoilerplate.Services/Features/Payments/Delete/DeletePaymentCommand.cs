using Inventory.Services.Features.Payments.Delete;
using MediatR;

namespace Inventory.Services.Features.Payments.Delete
{
    public class DeletePaymentCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeletePaymentCommand(Guid id)
        {
            Id = id;
        }
    }
}
