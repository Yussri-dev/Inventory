using Inventory.Dto.StockMouvements.Results;
using Inventory.Dto.StockMouvements.Requests;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.Create
{
    public class CreateStockMouvementCommand : IRequest<StockMouvementResult>
    {
        public CreateStockMouvementRequest Request { get; }

        public CreateStockMouvementCommand(CreateStockMouvementRequest request)
        {
            Request = request;
        }
    }
}
