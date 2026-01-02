
using Inventory.Dto.InventorySessions.Results;
using Inventory.Dto.InventorySessions.Requests;
using Inventory.Services.Features.InventorySessions.Create;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.Create
{
    public class CreateInventorySessionCommand : IRequest<InventorySessionResult>
    {
        public CreateInventorySessionRequest Request { get; }

        public CreateInventorySessionCommand(CreateInventorySessionRequest request)
        {
            Request = request;
        }
    }
}
