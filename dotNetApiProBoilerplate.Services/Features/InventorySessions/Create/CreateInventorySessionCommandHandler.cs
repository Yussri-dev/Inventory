
using Inventory.Dto.InventorySessions.Results;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.Create
{
    public class CreateInventorySessionCommandHandler : IRequestHandler<CreateInventorySessionCommand, InventorySessionResult>
    {
        private readonly InventorySessionService _customerService;

        public CreateInventorySessionCommandHandler(InventorySessionService productService)
        {
            _customerService = productService;
        }

        public Task<InventorySessionResult> Handle(CreateInventorySessionCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }
}
