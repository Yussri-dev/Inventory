using Inventory.Dto.InventorySessions.Results;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.GetAll
{
    public class GetAllInventorySessionsQuery : IRequest<List<InventorySessionResult>>
    {
    }
}
