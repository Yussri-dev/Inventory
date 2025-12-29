using Inventory.Dto.Stock.Results;
using MediatR;

namespace Inventory.Services.Features.Stocks.Create
{
    public class CreateStockCommandHandler : IRequestHandler<CreateStockCommand, StockResult>
    {
        private readonly StockService _productService;

        public CreateStockCommandHandler(StockService productService)
        {
            _productService = productService;
        }

        public Task<StockResult> Handle(CreateStockCommand command, CancellationToken cancellationToken)
        {
            // Minimal change: reuse your existing StockService logic
            return _productService.CreateAsync(command.Request);
        }
    }
}
