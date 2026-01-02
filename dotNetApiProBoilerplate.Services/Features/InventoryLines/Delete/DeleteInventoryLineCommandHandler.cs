using MediatR;

namespace Inventory.Services.Features.InventoryLines.Delete
{
    public class DeleteInventoryLineCommandHandler
         : IRequestHandler<DeleteInventoryLineCommand, Unit>
    {
        private readonly InventoryLineService _customerService;

        public DeleteInventoryLineCommandHandler(InventoryLineService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteInventoryLineCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
