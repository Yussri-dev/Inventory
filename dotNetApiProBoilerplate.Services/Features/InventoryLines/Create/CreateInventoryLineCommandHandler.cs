using Inventory.Dto.InventoryLines.Results;
using MediatR;

namespace Inventory.Services.Features.InventoryLines.Create
{
    public class CreateInventoryLineCommandHandler : IRequestHandler<CreateInventoryLineCommand, InventoryLineResult>
    {
        private readonly InventoryLineService _customerService;

        public CreateInventoryLineCommandHandler(InventoryLineService productService)
        {
            _customerService = productService;
        }

        public Task<InventoryLineResult> Handle(CreateInventoryLineCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }
}
