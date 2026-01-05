using Inventory.Dto.Auth.Results;
using MediatR;

namespace Inventory.Services.Features.Auth.Register
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResult>
    {
        private readonly AuthService _authService;

        public RegisterUserCommandHandler(AuthService authService)
        {
            _authService = authService;
        }

        public Task<AuthResult> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
        {
            return _authService.RegisterUserAsync(command.Request);
        }
    }
}
