using Inventory.Dto.Stock.Results;
using MediatR;

namespace Inventory.Services.Features.Stocks.GetAll
{
    public class GetAllStocksQueryHandler
        : IRequestHandler<GetAllStocksQuery, List<StockResult>>
    {
        private readonly StockService _productService;

        public GetAllStocksQueryHandler(StockService productService)
        {
            _productService = productService;
        }

        public Task<List<StockResult>> Handle(GetAllStocksQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetAllAsync();
        }
    }
}
