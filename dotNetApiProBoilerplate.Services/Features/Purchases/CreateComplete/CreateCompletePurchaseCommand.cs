using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Purchases.Results;
using MediatR;

namespace Inventory.Services.Features.Purchases.CreateComplete
{
    public class CreateCompletePurchaseCommand : IRequest<PurchaseResult>
    {
        public CreateCompletePurchaseRequest Request { get; }

        public CreateCompletePurchaseCommand(CreateCompletePurchaseRequest request)
        {
            Request = request;
        }
    }
}
