using Inventory.Dto.Auth.Requests;
using Inventory.Dto.Auth.Results;
using MediatR;

namespace Inventory.Services.Features.Auth.Register
{
    public class RegisterUserCommand : IRequest<AuthResult>
    {
        public RegisterUserRequest Request { get; } 
        public RegisterUserCommand(RegisterUserRequest request)
        {
            Request = request;
        }
    }

}
