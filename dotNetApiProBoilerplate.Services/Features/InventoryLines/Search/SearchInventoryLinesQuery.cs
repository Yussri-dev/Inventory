using Inventory.Dto.InventoryLines.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.InventoryLines.Search;
using MediatR;

namespace Inventory.Services.Features.InventoryLines.Search
{
    public class SearchInventoryLinesQuery : IRequest<PagedResult<InventoryLineResult>>
    {
        public InventoryLineQuery Query { get; }

        public SearchInventoryLinesQuery(InventoryLineQuery query)
        {
            Query = query;
        }
    }
}
