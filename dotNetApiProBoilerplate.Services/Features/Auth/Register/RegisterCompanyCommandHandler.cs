using Inventory.Dto.Auth.Results;
using MediatR;

namespace Inventory.Services.Features.Auth.Register
{
    public class RegisterCompanyCommandHandler : IRequestHandler<RegisterCompanyCommand, AuthResult>
    {
        private readonly AuthService _authService;

        public RegisterCompanyCommandHandler(AuthService authService)
        {
            _authService = authService;
        }

        public Task<AuthResult> Handle(RegisterCompanyCommand command, CancellationToken cancellationToken)
        {
            return _authService.RegisterCompanyAsync(command.Request);
        }
    }
}
