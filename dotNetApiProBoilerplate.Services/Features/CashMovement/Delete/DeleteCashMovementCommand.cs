using MediatR;

namespace Inventory.Services.Features.CashMovement.Delete
{
    public class DeleteCashMovementCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteCashMovementCommand(Guid id)
        {
            Id = id;
        }
    }
}
