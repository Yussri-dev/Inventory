

using Inventory.Dto.Stock.Results;
using MediatR;

namespace Inventory.Services.Features.Stocks.GetById
{
    public class GetStockByIdQueryHandler
        : IRequestHandler<GetStockByIdQuery, StockResult>
    {
        private readonly StockService _productService;

        public GetStockByIdQueryHandler(StockService productService)
        {
            _productService = productService;
        }

        public Task<StockResult> Handle(GetStockByIdQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetByIdAsync(query.Id);
        }
    }
}
