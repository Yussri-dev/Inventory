using MediatR;

namespace Inventory.Services.Features.PurchaseLines.Delete
{
    public class DeletePurchaseLineCommandHandler
       : IRequestHandler<DeletePurchaseLineCommand, Unit>
    {
        private readonly PurchaseLineService _productService;

        public DeletePurchaseLineCommandHandler(PurchaseLineService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeletePurchaseLineCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
