using Inventory.Dto.Users;
using Inventory.Services.Features.Users.Delete;
using MediatR;

namespace Inventory.Services.Features.Users.Delete
{
    public class DeleteUserCommand : IRequest<Unit>
    {
        public Guid Id { get; }

        public DeleteUserCommand(Guid id)
        {
            Id = id;
        }
    }

}
