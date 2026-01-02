using MediatR;

namespace Inventory.Services.Features.SupplierReturns.Delete
{
    public class DeleteSupplierReturnCommandHandler
         : IRequestHandler<DeleteSupplierReturnCommand, Unit>
    {
        private readonly SupplierReturnService _customerService;

        public DeleteSupplierReturnCommandHandler(SupplierReturnService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteSupplierReturnCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
