using MediatR;

namespace Inventory.Services.Features.InventorySessions.Close
{
    public class CloseInventorySessionCommandHandler
        : IRequestHandler<CloseInventorySessionCommand, bool>
    {
        private readonly InventorySessionService _service;

        public CloseInventorySessionCommandHandler(InventorySessionService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(
            CloseInventorySessionCommand request,
            CancellationToken cancellationToken)
        {
            return await _service.CloseAsync(request.Id);
        }
    }
}
