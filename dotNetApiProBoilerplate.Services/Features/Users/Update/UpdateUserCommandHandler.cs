using Inventory.Dto.Users;
using MediatR;

namespace Inventory.Services.Features.Users.Update
{
    public class UpdateUserCommandHandler
        :IRequestHandler<UpdateUserCommand,UserResult>
    {
        private readonly UserService _userService;

        public UpdateUserCommandHandler(UserService userService)
        {
            _userService = userService;
        }

        public Task<UserResult> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
        {
            return _userService.UpdateAsync(command.Id, command.Request);
        }
    }
}
