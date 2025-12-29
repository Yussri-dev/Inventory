using Inventory.Dto.PurchasePayments.Results;
using Inventory.Services.Features.PurchasePayments.GetById;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.GetById
{
    public class GetPurchasePaymentByIdQuery : IRequest<PurchasePaymentResult>
    {
        public Guid Id { get; }

        public GetPurchasePaymentByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
