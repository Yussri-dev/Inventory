using Inventory.Dto.StockMouvements.Results;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.GetAll
{
    public class GetAllStockMouvementsQueryHandler
       : IRequestHandler<GetAllStockMouvementsQuery, List<StockMouvementResult>>
    {
        private readonly StockMouvementService _productService;

        public GetAllStockMouvementsQueryHandler(StockMouvementService productService)
        {
            _productService = productService;
        }

        public Task<List<StockMouvementResult>> Handle(GetAllStockMouvementsQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetAllAsync();
        }
    }
}
