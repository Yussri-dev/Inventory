using Inventory.Dto.StockMouvements.Results;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.Create
{
    public class CreateStockMouvementCommandHandler : IRequestHandler<CreateStockMouvementCommand, StockMouvementResult>
    {
        private readonly StockMouvementService _productService;

        public CreateStockMouvementCommandHandler(StockMouvementService productService)
        {
            _productService = productService;
        }

        public Task<StockMouvementResult> Handle(CreateStockMouvementCommand command, CancellationToken cancellationToken)
        {
            // Minimal change: reuse your existing StockMouvementService logic
            return _productService.CreateAsync(command.Request);
        }
    }
}
