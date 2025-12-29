using Inventory.Dto.PurchaseLines.Results;
using MediatR;

namespace Inventory.Services.Features.PurchaseLines.Create
{
    public class CreatePurchaseLineCommandHandler : IRequestHandler<CreatePurchaseLineCommand, PurchaseLineResult>
    {
        private readonly PurchaseLineService _productService;

        public CreatePurchaseLineCommandHandler(PurchaseLineService productService)
        {
            _productService = productService;
        }

        public Task<PurchaseLineResult> Handle(CreatePurchaseLineCommand command, CancellationToken cancellationToken)
        {
            // Minimal change: reuse your existing PurchaseLineService logic
            return _productService.CreateAsync(command.Request);
        }
    }
}
