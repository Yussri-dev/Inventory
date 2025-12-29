using Inventory.Dto.StockMouvements.Results;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.GetAll
{
    public class GetAllStockMouvementsQuery : IRequest<List<StockMouvementResult>>
    {
    }
}
