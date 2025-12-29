using Inventory.Dto.PurchaseLines.Results;
using Inventory.Dto.PurchaseLines.Requests;
using MediatR;

namespace Inventory.Services.Features.PurchaseLines.Update
{
    public class UpdatePurchaseLineCommand : IRequest<PurchaseLineResult>
    {
        public Guid Id { get; }
        public UpdatePurchaseLineRequest Request { get; }

        public UpdatePurchaseLineCommand(Guid id, UpdatePurchaseLineRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
