using Inventory.Dto.PurchasePayments.Results;
using Inventory.Dto.PurchasePayments.Requests;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.Create
{
    public class CreatePurchasePaymentCommand : IRequest<PurchasePaymentResult>
    {
        public CreatePurchasePaymentRequest Request { get; }
        public CreatePurchasePaymentCommand(CreatePurchasePaymentRequest request)
        {
            Request = request;
        }
    }
}
