using Inventory.Services.Features.Purchases.Delete;
using MediatR;

namespace Inventory.Services.Features.Purchases.Delete
{
    public class DeletePurchaseCommand : IRequest<Unit>
    {
        public Guid Id { get; }

        public DeletePurchaseCommand(Guid id)
        {
            Id = id;
        }
    }
}
