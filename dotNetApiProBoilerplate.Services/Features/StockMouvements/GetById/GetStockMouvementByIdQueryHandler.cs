using Inventory.Dto.StockMouvements.Results;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.GetById
{
    public class GetStockMouvementByIdQueryHandler
        : IRequestHandler<GetStockMouvementByIdQuery, StockMouvementResult>
    {
        private readonly StockMouvementService _productService;

        public GetStockMouvementByIdQueryHandler(StockMouvementService productService)
        {
            _productService = productService;
        }

        public Task<StockMouvementResult> Handle(GetStockMouvementByIdQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetByIdAsync(query.Id);
        }
    }
}
