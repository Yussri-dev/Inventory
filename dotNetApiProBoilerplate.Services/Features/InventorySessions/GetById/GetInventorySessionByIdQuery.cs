using Inventory.Dto.InventorySessions.Results;
using Inventory.Services.Features.InventorySessions.GetById;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.GetById
{
    public class GetInventorySessionByIdQuery : IRequest<InventorySessionResult>
    {
        public Guid Id { get; }

        public GetInventorySessionByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
