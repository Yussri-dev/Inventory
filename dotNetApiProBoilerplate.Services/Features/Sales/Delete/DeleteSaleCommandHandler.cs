using MediatR;

namespace Inventory.Services.Features.Sales.Delete
{
    public class DeleteSaleCommandHandler
        : IRequestHandler<DeleteSaleCommand, Unit>
    {
        private readonly SaleService _productService;

        public DeleteSaleCommandHandler(SaleService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeleteSaleCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
