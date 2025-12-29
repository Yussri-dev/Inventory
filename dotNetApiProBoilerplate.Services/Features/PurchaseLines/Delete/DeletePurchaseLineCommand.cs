using Inventory.Services.Features.PurchaseLines.Delete;
using MediatR;

namespace Inventory.Services.Features.PurchaseLines.Delete
{
    public class DeletePurchaseLineCommand : IRequest<Unit>
    {
        public Guid Id { get; }

        public DeletePurchaseLineCommand(Guid id)
        {
            Id = id;
        }
    }
}
