using Inventory.Dto.InventorySessions.Results;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.GetAll
{
    public class GetAllInventorySessionsQueryHandler
  : IRequestHandler<GetAllInventorySessionsQuery, List<InventorySessionResult>>
    {
        private readonly InventorySessionService _customerService;

        public GetAllInventorySessionsQueryHandler(InventorySessionService customerService)
        {
            _customerService = customerService;
        }

        public Task<List<InventorySessionResult>> Handle(GetAllInventorySessionsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetAllAsync();
        }
    }
}
