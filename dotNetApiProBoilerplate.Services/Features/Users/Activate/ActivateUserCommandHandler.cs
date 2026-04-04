using Inventory.Dto.Users;
using MediatR;


namespace Inventory.Services.Features.Users.Activate
{
    public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand, UserResult>
    {
        private readonly UserService _userService;
        public ActivateUserCommandHandler(UserService userService)
        {
            _userService = userService;
        }
        public async Task<UserResult> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
        {
            return await _userService.DeactivateAsync(request.Id);
        }
    }
}
