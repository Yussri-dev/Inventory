using Inventory.Dto.Users;
using MediatR;

namespace Inventory.Services.Features.Users.Deactivate
{
    public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, UserResult>
    {
        private readonly UserService _userService;
        public DeactivateUserCommandHandler(UserService userService)
        {
            _userService = userService;
        }
        public async Task<UserResult> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            return await _userService.DeactivateAsync(request.Id);
        }
    }
}
