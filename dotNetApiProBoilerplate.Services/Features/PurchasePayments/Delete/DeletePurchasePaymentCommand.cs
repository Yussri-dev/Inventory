using Inventory.Services.Features.PurchasePayments.Delete;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.Delete
{
    public class DeletePurchasePaymentCommand : IRequest<Unit>
    {
        public Guid Id { get; }

        public DeletePurchasePaymentCommand(Guid id)
        {
            Id = id;
        }
    }
}
