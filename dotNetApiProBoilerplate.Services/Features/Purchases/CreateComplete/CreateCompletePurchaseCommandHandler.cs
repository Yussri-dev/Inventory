using Inventory.Dto.Purchases.Results;
using MediatR;

namespace Inventory.Services.Features.Purchases.CreateComplete
{
    public class CreateCompletePurchaseCommandHandler : IRequestHandler<CreateCompletePurchaseCommand, PurchaseResult>
    {
        private readonly PurchaseService _service;

        public CreateCompletePurchaseCommandHandler(PurchaseService service)
        {
            _service = service;
        }

        public async Task<PurchaseResult> Handle(CreateCompletePurchaseCommand command, CancellationToken cancellationToken)
        {
            return await _service.CreateCompleteAsync(command.Request);
        }
    }
}
