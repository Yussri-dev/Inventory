using Inventory.Dto.InventorySessions.Results;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.Update
{
    public class UpdateInventorySessionCommandHandler
       : IRequestHandler<UpdateInventorySessionCommand, InventorySessionResult>
    {
        private readonly InventorySessionService _customerService;

        public UpdateInventorySessionCommandHandler(InventorySessionService customerService)
        {
            _customerService = customerService;
        }

        public Task<InventorySessionResult> Handle(UpdateInventorySessionCommand command, CancellationToken cancellationToken)
        {
            return _customerService.UpdateAsync(command.Id, command.Request);
        }
    }

}
