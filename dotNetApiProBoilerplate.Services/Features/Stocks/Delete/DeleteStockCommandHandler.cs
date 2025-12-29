using MediatR;

namespace Inventory.Services.Features.Stocks.Delete
{
    public class DeleteStockCommandHandler
        : IRequestHandler<DeleteStockCommand, Unit>
    {
        private readonly StockService _productService;

        public DeleteStockCommandHandler(StockService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeleteStockCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
