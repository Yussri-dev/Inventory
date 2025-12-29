using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.SaleLines.Results;
using Inventory.Services.Features.SaleLines.Search;
using MediatR;

namespace Inventory.Services.Features.SaleLines.Search
{
    public class SearchSaleLinesQuery : IRequest<PagedResult<SaleLineResult>>
    {
        public SaleLineQuery Query { get; }

        public SearchSaleLinesQuery(SaleLineQuery query)
        {
            Query = query;
        }
    }
}
