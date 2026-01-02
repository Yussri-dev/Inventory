using MediatR;

namespace Inventory.Services.Features.InventorySessions.Delete
{
    public class DeleteInventorySessionCommandHandler
         : IRequestHandler<DeleteInventorySessionCommand, Unit>
    {
        private readonly InventorySessionService _customerService;

        public DeleteInventorySessionCommandHandler(InventorySessionService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteInventorySessionCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
