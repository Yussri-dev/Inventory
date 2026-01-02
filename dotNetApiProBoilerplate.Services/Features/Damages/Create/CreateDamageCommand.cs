
using Inventory.Dto.Damages.Requests;
using Inventory.Dto.Damages.Results;
using MediatR;

namespace Inventory.Services.Features.Damages.Create
{
    public class CreateDamageCommand : IRequest<DamageResult>
    {
        public CreateDamageRequest Request { get; }

        public CreateDamageCommand(CreateDamageRequest request)
        {
            Request = request;
        }
    }
}
