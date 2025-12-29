using Inventory.Dto.Stock.Results;
using MediatR;


namespace Inventory.Services.Features.Stocks.Update
{
    public class UpdateStockCommandHandler
       : IRequestHandler<UpdateStockCommand, StockResult>
    {
        private readonly StockService _productService;

        public UpdateStockCommandHandler(StockService productService)
        {
            _productService = productService;
        }

        public Task<StockResult> Handle(UpdateStockCommand command, CancellationToken cancellationToken)
        {
            return _productService.UpdateAsync(command.Id, command.Request);
        }
    }
}
