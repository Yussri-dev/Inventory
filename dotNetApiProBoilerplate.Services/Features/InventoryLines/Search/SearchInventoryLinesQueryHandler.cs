using Inventory.Dto.InventoryLines.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.InventoryLines.Search
{
    public class SearchInventoryLinesQueryHandler
    : IRequestHandler<SearchInventoryLinesQuery, PagedResult<InventoryLineResult>>
    {
        private readonly InventoryLineService _customerService;

        public SearchInventoryLinesQueryHandler(InventoryLineService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<InventoryLineResult>> Handle(SearchInventoryLinesQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
