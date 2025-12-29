using Inventory.Dto.Suppliers.Results;
using MediatR;

namespace Inventory.Services.Features.Suppliers.GetAll
{
    public class GetAllSuppliersQuery : IRequest<List<SupplierResult>>
    {
    }
}
