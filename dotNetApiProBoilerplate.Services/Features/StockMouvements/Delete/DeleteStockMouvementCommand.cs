using Inventory.Services.Features.StockMouvements.Delete;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.Delete
{
    public class DeleteStockMouvementCommand : IRequest<Unit>
    {
        public Guid Id { get; }

        public DeleteStockMouvementCommand(Guid id)
        {
            Id = id;
        }
    }
}
