using Inventory.Dto.Damages.Requests;
using Inventory.Dto.Damages.Results;
using MediatR;

namespace Inventory.Services.Features.Damages.Update
{
    public class UpdateDamageCommand : IRequest<DamageResult>
    {
        public Guid Id { get; }
        public UpdateDamageRequest Request { get; }

        public UpdateDamageCommand(Guid id, UpdateDamageRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
