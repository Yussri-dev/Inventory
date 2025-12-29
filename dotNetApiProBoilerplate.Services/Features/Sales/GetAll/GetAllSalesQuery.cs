using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.GetAll
{
    public class GetAllSalesQuery : IRequest<List<SaleResult>>
    {
    }
}
