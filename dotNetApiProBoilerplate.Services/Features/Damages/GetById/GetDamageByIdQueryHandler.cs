using Inventory.Dto.Damages.Results;
using MediatR;

namespace Inventory.Services.Features.Damages.GetById
{
    public class GetDamageByIdQueryHandler
        : IRequestHandler<GetDamageByIdQuery, DamageResult>
    {
        private readonly DamageService _customerService;

        public GetDamageByIdQueryHandler(DamageService customerService)
        {
            _customerService = customerService;
        }

        public Task<DamageResult> Handle(GetDamageByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
