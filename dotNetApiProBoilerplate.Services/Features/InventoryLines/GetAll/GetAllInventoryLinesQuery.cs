using Inventory.Dto.InventoryLines.Results;
using MediatR;

namespace Inventory.Services.Features.InventoryLines.GetAll
{
    public class GetAllInventoryLinesQuery : IRequest<List<InventoryLineResult>>
    {
    }
}
