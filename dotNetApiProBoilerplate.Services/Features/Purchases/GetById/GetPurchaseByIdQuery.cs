using Inventory.Dto.Purchases.Results;
using MediatR;

namespace Inventory.Services.Features.Purchases.GetById
{
    public class GetPurchaseByIdQuery : IRequest<PurchaseResult>
    {
        public Guid Id { get; }

        public GetPurchaseByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
