using MediatR;

namespace Inventory.Services.Features.Purchases.Delete
{
    public class DeletePurchaseCommandHandler
        : IRequestHandler<DeletePurchaseCommand, Unit>
    {
        private readonly PurchaseService _productService;

        public DeletePurchaseCommandHandler(PurchaseService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeletePurchaseCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
