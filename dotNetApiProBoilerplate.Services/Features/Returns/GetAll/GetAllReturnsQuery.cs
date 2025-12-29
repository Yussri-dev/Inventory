using Inventory.Dto.Returns.Results;
using Inventory.Services.Features.Returns.GetAll;
using MediatR;

namespace Inventory.Services.Features.Returns.GetAll
{
    public class GetAllReturnsQuery : IRequest<List<ReturnResult>>
    {
    }
}
