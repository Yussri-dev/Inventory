using MediatR;

namespace Inventory.Services.Features.Returns.Delete
{
    public class DeleteReturnCommand : IRequest<Unit>
    {
        public Guid Id { get; }

        public DeleteReturnCommand(Guid id)
        {
            Id = id;
        }
    }
}
