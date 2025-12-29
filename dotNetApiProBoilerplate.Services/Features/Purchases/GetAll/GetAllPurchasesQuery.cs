using Inventory.Dto.Purchases.Results;
using MediatR;

namespace Inventory.Services.Features.Purchases.GetAll
{
    public class GetAllPurchasesQuery : IRequest<List<PurchaseResult>>
    {
    }
}
