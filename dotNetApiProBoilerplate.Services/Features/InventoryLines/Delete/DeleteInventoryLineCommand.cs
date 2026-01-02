using Inventory.Services.Features.InventoryLines.Delete;
using MediatR;

namespace Inventory.Services.Features.InventoryLines.Delete
{
    public class DeleteInventoryLineCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteInventoryLineCommand(Guid id)
        {
            Id = id;
        }
    }
}
