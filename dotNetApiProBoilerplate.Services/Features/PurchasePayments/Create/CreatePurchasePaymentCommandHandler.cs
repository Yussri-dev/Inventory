using Inventory.Dto.PurchasePayments.Results;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.Create
{
    public class CreatePurchasePaymentCommandHandler : IRequestHandler<CreatePurchasePaymentCommand, PurchasePaymentResult>
    {
        private readonly PurchasePaymentService _productService;

        public CreatePurchasePaymentCommandHandler(PurchasePaymentService productService)
        {
            _productService = productService;
        }

        public Task<PurchasePaymentResult> Handle(CreatePurchasePaymentCommand command, CancellationToken cancellationToken)
        {
            // Minimal change: reuse your existing PurchasePaymentService logic
            return _productService.CreateAsync(command.Request);
        }
    }
}
