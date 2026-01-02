using Inventory.Dto.Damages.Results;
using MediatR;

namespace Inventory.Services.Features.Damages.GetAll
{
    public class GetAllDamagesQueryHandler
   : IRequestHandler<GetAllDamagesQuery, List<DamageResult>>
    {
        private readonly DamageService _customerService;

        public GetAllDamagesQueryHandler(DamageService customerService)
        {
            _customerService = customerService;
        }

        public Task<List<DamageResult>> Handle(GetAllDamagesQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetAllAsync();
        }
    }
}
