using MediatR;

namespace Inventory.Services.Features.PurchasePayments.Delete
{
    public class DeletePurchasePaymentCommandHandler
       : IRequestHandler<DeletePurchasePaymentCommand, Unit>
    {
        private readonly PurchasePaymentService _productService;

        public DeletePurchasePaymentCommandHandler(PurchasePaymentService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeletePurchasePaymentCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
