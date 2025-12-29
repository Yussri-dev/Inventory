using Inventory.Dto.StockMouvements.Results;
using Inventory.Services.Features.StockMouvements.GetById;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.GetById
{
    public class GetStockMouvementByIdQuery : IRequest<StockMouvementResult>
    {
        public Guid Id { get; }

        public GetStockMouvementByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
