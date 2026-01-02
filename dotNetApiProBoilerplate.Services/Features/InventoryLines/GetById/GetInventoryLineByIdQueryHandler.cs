using Inventory.Dto.InventoryLines.Results;
using MediatR;

namespace Inventory.Services.Features.InventoryLines.GetById
{
    public class GetInventoryLineByIdQueryHandler
       : IRequestHandler<GetInventoryLineByIdQuery, InventoryLineResult>
    {
        private readonly InventoryLineService _customerService;

        public GetInventoryLineByIdQueryHandler(InventoryLineService customerService)
        {
            _customerService = customerService;
        }

        public Task<InventoryLineResult> Handle(GetInventoryLineByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
