using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Purchases.Requests;
using Inventory.Services.Features.Purchases.Update;
using MediatR;

namespace Inventory.Services.Features.Purchases.Update
{
    public class UpdatePurchaseCommand : IRequest<PurchaseResult>
    {
        public Guid Id { get; }
        public UpdatePurchaseRequest Request { get; }

        public UpdatePurchaseCommand(Guid id, UpdatePurchaseRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
