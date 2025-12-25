using Inventory.Dto.Purchases.Results;
using MediatR;

namespace Inventory.Services.Features.Purchases.Update
{
    public class UpdatePurchaseCommandHandler
       : IRequestHandler<UpdatePurchaseCommand, PurchaseResult>
    {
        private readonly PurchaseService _productService;

        public UpdatePurchaseCommandHandler(PurchaseService productService)
        {
            _productService = productService;
        }

        public Task<PurchaseResult> Handle(UpdatePurchaseCommand command, CancellationToken cancellationToken)
        {
            return _productService.UpdateAsync(command.Id, command.Request);
        }
    }
}
