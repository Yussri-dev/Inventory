using Inventory.Dto.Auth.Results;
using MediatR;

namespace Inventory.Services.Features.Auth.Login
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, AuthResult>
    {
        private readonly AuthService _authService;

        public LoginUserCommandHandler(AuthService authService)
        {
            _authService = authService;
        }

        public Task<AuthResult> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            return _authService.LoginAsync(command.Request);
        }
    }
}
