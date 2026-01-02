using Inventory.Dto.InventorySessions.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.Search
{
    public class SearchInventorySessionsQueryHandler
    : IRequestHandler<SearchInventorySessionsQuery, PagedResult<InventorySessionResult>>
    {
        private readonly InventorySessionService _customerService;

        public SearchInventorySessionsQueryHandler(InventorySessionService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<InventorySessionResult>> Handle(SearchInventorySessionsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
