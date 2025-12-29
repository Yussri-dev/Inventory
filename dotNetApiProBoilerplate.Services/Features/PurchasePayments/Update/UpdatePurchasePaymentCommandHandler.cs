using Inventory.Dto.PurchasePayments.Results;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.Update
{
    public class UpdatePurchasePaymentCommandHandler
       : IRequestHandler<UpdatePurchasePaymentCommand, PurchasePaymentResult>
    {
        private readonly PurchasePaymentService _productService;

        public UpdatePurchasePaymentCommandHandler(PurchasePaymentService productService)
        {
            _productService = productService;
        }

        public Task<PurchasePaymentResult> Handle(UpdatePurchasePaymentCommand command, CancellationToken cancellationToken)
        {
            return _productService.UpdateAsync(command.Id, command.Request);
        }
    }
}
