using MediatR;

namespace Inventory.Services.Features.Auth.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
    {
        private readonly AuthService _authService;

        public ChangePasswordCommandHandler(AuthService authService)
        {
            _authService = authService;
        }

        public async Task<Unit> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            await _authService.ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword);
            return Unit.Value;
        }
    }
}
