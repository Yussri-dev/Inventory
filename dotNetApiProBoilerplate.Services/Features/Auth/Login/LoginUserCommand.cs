using Inventory.Dto.Auth.Requests;
using Inventory.Dto.Auth.Results;
using MediatR;

namespace Inventory.Services.Features.Auth.Login
{
    public class LoginUserCommand : IRequest<AuthResult>
    {
        public LoginRequest Request { get; }

        public LoginUserCommand(LoginRequest request)
        {
            Request = request;
        }
    }
}
