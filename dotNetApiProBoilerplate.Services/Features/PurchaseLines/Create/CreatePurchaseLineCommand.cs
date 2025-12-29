using Inventory.Dto.PurchaseLines.Results;
using Inventory.Dto.PurchaseLines.Requests;
using Inventory.Services.Features.PurchaseLines.Create;
using MediatR;

namespace Inventory.Services.Features.PurchaseLines.Create
{
    public class CreatePurchaseLineCommand : IRequest<PurchaseLineResult>
    {
        public CreatePurchaseLineRequest Request { get; }

        public CreatePurchaseLineCommand(CreatePurchaseLineRequest request)
        {
            Request = request;
        }
    }
}
