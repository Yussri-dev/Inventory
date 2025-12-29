using Inventory.Dto.Suppliers.Results;
using MediatR;

namespace Inventory.Services.Features.Suppliers.Update
{
    public class UpdateSupplierCommandHandler
    : IRequestHandler<UpdateSupplierCommand, SupplierResult>
    {
        private readonly SupplierService _customerService;

        public UpdateSupplierCommandHandler(SupplierService customerService)
        {
            _customerService = customerService;
        }

        public Task<SupplierResult> Handle(UpdateSupplierCommand command, CancellationToken cancellationToken)
        {
            return _customerService.UpdateAsync(command.Id, command.Request);
        }
    }
}
