using Inventory.Dto.Sales.Results;
using MediatR;


namespace Inventory.Services.Features.Sales.GetPending
{
    public class GetAllPendingSalesQuery : IRequest<List<SaleResult>>
    {
    }
}
