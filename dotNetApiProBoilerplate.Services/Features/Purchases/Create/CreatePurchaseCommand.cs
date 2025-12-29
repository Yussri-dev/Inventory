using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Purchases.Requests;
using MediatR;

namespace Inventory.Services.Features.Purchases.Create
{
    public class CreatePurchaseCommand : IRequest<PurchaseResult>
    {
        public CreatePurchaseRequest Request { get; }

        public CreatePurchaseCommand(CreatePurchaseRequest request)
        {
            Request = request;
        }
    }
}
