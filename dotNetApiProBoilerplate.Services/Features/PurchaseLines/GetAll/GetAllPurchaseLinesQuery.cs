using Inventory.Dto.PurchaseLines.Results;
using MediatR;


namespace Inventory.Services.Features.PurchaseLines.GetAll
{
    public class GetAllPurchaseLinesQuery : IRequest<List<PurchaseLineResult>>
    {
    }
}
