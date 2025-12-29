using MediatR;

namespace Inventory.Services.Features.StockMouvements.Delete
{
    public class DeleteStockMouvementCommandHandler
        : IRequestHandler<DeleteStockMouvementCommand, Unit>
    {
        private readonly StockMouvementService _productService;

        public DeleteStockMouvementCommandHandler(StockMouvementService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeleteStockMouvementCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
