using Inventory.Services.Features.Damages.Delete;
using MediatR;


namespace Inventory.Services.Features.Damages.Delete
{
    public class DeleteDamageCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteDamageCommand(Guid id)
        {
            Id = id;
        }
    }
}
