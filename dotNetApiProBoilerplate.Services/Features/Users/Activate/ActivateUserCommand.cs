using Inventory.Dto.Users;
using MediatR;


namespace Inventory.Services.Features.Users.Activate
{
    public class ActivateUserCommand : IRequest<UserResult>
    {
        public Guid Id { get; }
        public ActivateUserCommand(Guid id)
        {
            Id = id;
        }
    }
}
