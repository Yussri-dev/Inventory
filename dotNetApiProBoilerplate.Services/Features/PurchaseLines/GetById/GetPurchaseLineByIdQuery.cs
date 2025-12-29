using Inventory.Dto.PurchaseLines.Results;
using MediatR;

namespace Inventory.Services.Features.PurchaseLines.GetById
{
    public class GetPurchaseLineByIdQuery : IRequest<PurchaseLineResult>
    {
        public Guid Id { get; }

        public GetPurchaseLineByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
