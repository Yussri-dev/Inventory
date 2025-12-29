using Inventory.Dto.Suppliers.Results;
using MediatR;

namespace Inventory.Services.Features.Suppliers.Create
{
    public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, SupplierResult>
    {
        private readonly SupplierService _customerService;

        public CreateSupplierCommandHandler(SupplierService productService)
        {
            _customerService = productService;
        }

        public Task<SupplierResult> Handle(CreateSupplierCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }
}
