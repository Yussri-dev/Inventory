using Inventory.Dto.SupplierReturns.Results;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.CreateComplete
{
    public class CreateCompleteSupplierReturnCommandHandler : IRequestHandler<CreateCompleteSupplierReturnCommand, SupplierReturnResult>
    {
        private readonly SupplierReturnService _service;

        public CreateCompleteSupplierReturnCommandHandler(SupplierReturnService service)
        {
            _service = service;
        }

        public async Task<SupplierReturnResult> Handle(CreateCompleteSupplierReturnCommand command, CancellationToken cancellationToken)
        {
            return await _service.CreateCompleteAsync(command.Request);
        }
    }
}
