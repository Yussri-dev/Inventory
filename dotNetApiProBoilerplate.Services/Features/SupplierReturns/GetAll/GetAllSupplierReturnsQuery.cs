using Inventory.Dto.SupplierReturns.Results;
using Inventory.Services.Features.SupplierReturns.GetAll;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.GetAll
{
    public class GetAllSupplierReturnsQuery : IRequest<List<SupplierReturnResult>>
    {
    }
}
