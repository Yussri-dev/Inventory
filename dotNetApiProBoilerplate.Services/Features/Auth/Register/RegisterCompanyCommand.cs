using Inventory.Dto.Auth.Requests;
using Inventory.Dto.Auth.Results;
using MediatR;

namespace Inventory.Services.Features.Auth.Register
{
    public class RegisterCompanyCommand : IRequest<AuthResult>
    {
        public RegisterCompanyRequest Request { get; }

        public RegisterCompanyCommand(RegisterCompanyRequest request)
        {
            Request = request;
        }
    }
}
