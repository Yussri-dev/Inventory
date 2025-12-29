using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.StockMouvements.Results;
using Inventory.Services.Features.StockMouvements.Search;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.Search
{
    public class SearchStockMouvementsQuery : IRequest<PagedResult<StockMouvementResult>>
    {
        public StockMouvementQuery Query { get; }

        public SearchStockMouvementsQuery(StockMouvementQuery query)
        {
            Query = query;
        }
    }
}
