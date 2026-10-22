using Inventory.Dto.Auth.Results;
using MediatR;

namespace Inventory.Services.Features.Auth.Refresh
{
    public sealed record RefreshTokenCommand(
        string RefreshToken)
        : IRequest<AuthResult>;
}
