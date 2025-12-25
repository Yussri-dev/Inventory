using Inventory.Dto.Purchases.Results;
using MediatR;

namespace Inventory.Services.Features.Purchases.Create
{
    public class CreatePurchaseCommandHandler : IRequestHandler<CreatePurchaseCommand, PurchaseResult>
    {
        private readonly PurchaseService _productService;

        public CreatePurchaseCommandHandler(PurchaseService productService)
        {
            _productService = productService;
        }

        public Task<PurchaseResult> Handle(CreatePurchaseCommand command, CancellationToken cancellationToken)
        {
            // Minimal change: reuse your existing PurchaseService logic
            return _productService.CreateAsync(command.Request);
        }
    }
}
