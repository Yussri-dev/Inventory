using Inventory.Dto.StockMouvements.Results;
using Inventory.Dto.StockMouvements.Requests;
using Inventory.Services.Features.StockMouvements.Update;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.Update
{
    public class UpdateStockMouvementCommand : IRequest<StockMouvementResult>
    {
        public Guid Id { get; }
        public UpdateStockMouvementRequest Request { get; }

        public UpdateStockMouvementCommand(Guid id, UpdateStockMouvementRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
