using Inventory.Dto.Damages.Results;
using Inventory.Services.Features.Damages.GetAll;
using MediatR;

namespace Inventory.Services.Features.Damages.GetAll
{
    public class GetAllDamagesQuery : IRequest<List<DamageResult>>
    {
    }
}
