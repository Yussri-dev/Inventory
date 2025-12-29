using MediatR;

namespace Inventory.Services.Features.Suppliers.Delete
{
    public class DeleteSupplierCommandHandler
         : IRequestHandler<DeleteSupplierCommand, Unit>
    {
        private readonly SupplierService _customerService;

        public DeleteSupplierCommandHandler(SupplierService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteSupplierCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
