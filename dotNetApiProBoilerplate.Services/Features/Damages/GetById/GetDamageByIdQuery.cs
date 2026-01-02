using Inventory.Dto.Damages.Results;
using MediatR;

namespace Inventory.Services.Features.Damages.GetById
{
    public class GetDamageByIdQuery : IRequest<DamageResult>
    {
        public Guid Id { get; }

        public GetDamageByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
