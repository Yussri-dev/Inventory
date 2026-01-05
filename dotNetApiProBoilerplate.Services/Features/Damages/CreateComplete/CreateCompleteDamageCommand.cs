using Inventory.Dto.Damages.Requests;
using Inventory.Dto.Damages.Results;
using MediatR;

namespace Inventory.Services.Features.Damages.CreateComplete
{
    public class CreateCompleteDamageCommand : IRequest<DamageResult>
    {
        public CreateCompleteDamageRequest Request { get; }

        public CreateCompleteDamageCommand(CreateCompleteDamageRequest request)
        {
            Request = request;
        }
    }
}
