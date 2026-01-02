using Inventory.Dto.InventorySessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.InventorySessions.Search;
using MediatR;

namespace Inventory.Services.Features.InventorySessions.Search
{
    public class SearchInventorySessionsQuery : IRequest<PagedResult<InventorySessionResult>>
    {
        public InventorySessionQuery Query { get; }

        public SearchInventorySessionsQuery(InventorySessionQuery query)
        {
            Query = query;
        }
    }
}
