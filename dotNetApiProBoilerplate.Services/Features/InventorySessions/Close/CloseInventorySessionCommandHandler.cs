using MediatR;

namespace Inventory.Services.Features.InventorySessions.Close;

public sealed class CloseInventorySessionCommandHandler
    : IRequestHandler<CloseInventorySessionCommand, bool>
{
    private readonly InventorySessionService _service;

    public CloseInventorySessionCommandHandler(
        InventorySessionService service)
    {
        _service =
            service;
    }

    public Task<bool> Handle(
        CloseInventorySessionCommand request,
        CancellationToken cancellationToken)
    {
        return _service.CloseAsync(
            request.Id,
            cancellationToken);
    }
}