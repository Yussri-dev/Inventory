
using Inventory.Dto.PurchaseLines.Results;
using MediatR;

namespace Inventory.Services.Features.PurchaseLines.Update
{
    public class UpdatePurchaseLineCommandHandler
       : IRequestHandler<UpdatePurchaseLineCommand, PurchaseLineResult>
    {
        private readonly PurchaseLineService _productService;

        public UpdatePurchaseLineCommandHandler(PurchaseLineService productService)
        {
            _productService = productService;
        }

        public Task<PurchaseLineResult> Handle(UpdatePurchaseLineCommand command, CancellationToken cancellationToken)
        {
            return _productService.UpdateAsync(command.Id, command.Request);
        }
    }
}
