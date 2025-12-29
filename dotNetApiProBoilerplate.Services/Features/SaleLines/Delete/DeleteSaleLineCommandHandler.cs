using MediatR;

namespace Inventory.Services.Features.SaleLines.Delete
{
    public class DeleteSaleLineCommandHandler
       : IRequestHandler<DeleteSaleLineCommand, Unit>
    {
        private readonly SaleLineService _productService;

        public DeleteSaleLineCommandHandler(SaleLineService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeleteSaleLineCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
