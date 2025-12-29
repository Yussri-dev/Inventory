using Inventory.Dto.StockMouvements.Results;
using MediatR;

namespace Inventory.Services.Features.StockMouvements.Update
{
    public class UpdateStockMouvementCommandHandler
       : IRequestHandler<UpdateStockMouvementCommand, StockMouvementResult>
    {
        private readonly StockMouvementService _productService;

        public UpdateStockMouvementCommandHandler(StockMouvementService productService)
        {
            _productService = productService;
        }

        public Task<StockMouvementResult> Handle(UpdateStockMouvementCommand command, CancellationToken cancellationToken)
        {
            return _productService.UpdateAsync(command.Id, command.Request);
        }
    }
}
