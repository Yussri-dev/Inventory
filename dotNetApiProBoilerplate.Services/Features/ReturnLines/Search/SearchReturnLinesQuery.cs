using Inventory.Dto.Pages.Results;
using Inventory.Dto.ReturnLines.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.ReturnLines.Search;
using MediatR;

namespace Inventory.Services.Features.ReturnLines.Search
{
    public class SearchReturnLinesQuery : IRequest<PagedResult<ReturnLineResult>>
    {
        public ReturnLineQuery Query { get; }

        public SearchReturnLinesQuery(ReturnLineQuery query)
        {
            Query = query;
        }
    }
}
