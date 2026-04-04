using MediatR;

namespace Inventory.Services.Features.InventorySessions.Validate
{
    public class ValidateInventorySessionCommandHandler
       : IRequestHandler<ValidateInventorySessionCommand, bool>
    {
        private readonly InventorySessionService _service;

        public ValidateInventorySessionCommandHandler(InventorySessionService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(
            ValidateInventorySessionCommand request,
            CancellationToken cancellationToken)
        {
            return await _service.ValidateAsync(request.Id);
        }
    }
}
