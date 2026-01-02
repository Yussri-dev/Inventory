using Inventory.Services.Features.InventorySessions.Delete;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.Delete
{
    public class DeleteInventorySessionCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteInventorySessionCommand(Guid id)
        {
            Id = id;
        }
    }
}
