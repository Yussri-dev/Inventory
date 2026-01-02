using Inventory.Dto.InventoryLines.Results;
using MediatR;

namespace Inventory.Services.Features.InventoryLines.Update
{
    public class UpdateInventoryLineCommandHandler
       : IRequestHandler<UpdateInventoryLineCommand, InventoryLineResult>
    {
        private readonly InventoryLineService _customerService;

        public UpdateInventoryLineCommandHandler(InventoryLineService customerService)
        {
            _customerService = customerService;
        }

        public Task<InventoryLineResult> Handle(UpdateInventoryLineCommand command, CancellationToken cancellationToken)
        {
            return _customerService.UpdateAsync(command.Id, command.Request);
        }
    }
}
