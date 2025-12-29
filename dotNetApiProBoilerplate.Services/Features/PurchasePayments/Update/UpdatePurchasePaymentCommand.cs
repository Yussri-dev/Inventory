using Inventory.Dto.PurchasePayments.Results;
using Inventory.Dto.PurchasePayments.Requests;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.Update
{
    public class UpdatePurchasePaymentCommand : IRequest<PurchasePaymentResult>
    {
        public Guid Id { get; }
        public UpdatePurchasePaymentRequest Request { get; }

        public UpdatePurchasePaymentCommand(Guid id, UpdatePurchasePaymentRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
