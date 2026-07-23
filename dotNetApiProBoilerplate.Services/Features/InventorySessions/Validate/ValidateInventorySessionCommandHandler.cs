using MediatR;

namespace Inventory.Services.Features.InventorySessions.Validate;

public sealed class ValidateInventorySessionCommandHandler
    : IRequestHandler<ValidateInventorySessionCommand, bool>
{
    private readonly InventorySessionService _service;

    public ValidateInventorySessionCommandHandler(
        InventorySessionService service)
    {
        _service =
            service;
    }

    public Task<bool> Handle(
        ValidateInventorySessionCommand request,
        CancellationToken cancellationToken)
    {
        return _service.ValidateAsync(
            request.Id,
            cancellationToken);
    }
}