using Inventory.Dto.InventoryLines.Results;
using Inventory.Services.Features.InventoryLines.GetById;
using MediatR;

namespace Inventory.Services.Features.InventoryLines.GetById
{
    public class GetInventoryLineByIdQuery : IRequest<InventoryLineResult>
    {
        public Guid Id { get; }

        public GetInventoryLineByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
