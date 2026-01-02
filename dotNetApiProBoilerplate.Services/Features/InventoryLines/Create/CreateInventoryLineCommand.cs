using Inventory.Dto.InventoryLines.Requests;
using Inventory.Dto.InventoryLines.Results;
using MediatR;

namespace Inventory.Services.Features.InventoryLines.Create
{
    public class CreateInventoryLineCommand : IRequest<InventoryLineResult>
    {
        public CreateInventoryLineRequest Request { get; }

        public CreateInventoryLineCommand(CreateInventoryLineRequest request)
        {
            Request = request;
        }
    }
}
