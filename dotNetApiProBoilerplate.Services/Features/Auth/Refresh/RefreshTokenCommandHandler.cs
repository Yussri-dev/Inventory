using Inventory.Dto.Auth.Results;
using MediatR;

namespace Inventory.Services.Features.Auth.Refresh
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
    {
        private readonly AuthService _authService;

        public RefreshTokenCommandHandler(AuthService authService)
        {
            _authService = authService;
        }

        public Task<AuthResult> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
        {
            return _authService.RefreshTokenAsync(command.UserId);
        }
    }
}
