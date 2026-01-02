using Inventory.Dto.InventorySessions.Results;
using Inventory.Dto.InventorySessions.Requests;
using Inventory.Services.Features.InventorySessions.Update;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.Update
{
    public class UpdateInventorySessionCommand : IRequest<InventorySessionResult>
    {
        public Guid Id { get; }
        public UpdateInventorySessionRequest Request { get; }

        public UpdateInventorySessionCommand(Guid id, UpdateInventorySessionRequest request)
        {
            Id = id;
            Request = request;
        }
    }

}
