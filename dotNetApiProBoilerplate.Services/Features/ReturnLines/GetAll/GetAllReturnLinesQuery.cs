using Inventory.Dto.ReturnLines.Results;
using Inventory.Services.Features.ReturnLines.GetAll;
using MediatR;

namespace Inventory.Services.Features.ReturnLines.GetAll
{
    public class GetAllReturnLinesQuery : IRequest<List<ReturnLineResult>>
    {
    }
}
