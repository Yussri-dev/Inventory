using Inventory.Dto.SupplierReturns.Results;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.Create
{
    public class CreateSupplierReturnCommandHandler : IRequestHandler<CreateSupplierReturnCommand, SupplierReturnResult>
    {
        private readonly SupplierReturnService _customerService;

        public CreateSupplierReturnCommandHandler(SupplierReturnService productService)
        {
            _customerService = productService;
        }

        public Task<SupplierReturnResult> Handle(CreateSupplierReturnCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }

}
