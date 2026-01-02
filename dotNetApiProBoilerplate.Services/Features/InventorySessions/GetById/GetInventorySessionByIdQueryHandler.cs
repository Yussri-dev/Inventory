using Inventory.Dto.InventorySessions.Results;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.GetById
{
    public class GetInventorySessionByIdQueryHandler
       : IRequestHandler<GetInventorySessionByIdQuery, InventorySessionResult>
    {
        private readonly InventorySessionService _customerService;

        public GetInventorySessionByIdQueryHandler(InventorySessionService customerService)
        {
            _customerService = customerService;
        }

        public Task<InventorySessionResult> Handle(GetInventorySessionByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
