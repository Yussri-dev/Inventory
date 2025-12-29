using Inventory.Services.Features.ReturnLines.Delete;
using MediatR;


namespace Inventory.Services.Features.ReturnLines.Delete
{
    public class DeleteReturnLineCommand : IRequest<Unit>
    {
        public Guid Id { get; }

        public DeleteReturnLineCommand(Guid id)
        {
            Id = id;
        }
    }
}
