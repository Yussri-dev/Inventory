using Inventory.Dto.Auth.Results;
using MediatR;

namespace Inventory.Services.Features.Auth.Refresh
{
    public class RefreshTokenCommand : IRequest<AuthResult>
    {
        public string UserId { get; }

        public RefreshTokenCommand(string userId)
        {
            UserId = userId;
        }
    }
}
