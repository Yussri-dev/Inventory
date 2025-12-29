using Inventory.Dto.Pages.Results;
using Inventory.Dto.Returns.Results;
using Inventory.Dto.Queries;
using MediatR;

namespace Inventory.Services.Features.Returns.Search
{
    public class SearchReturnsQuery : IRequest<PagedResult<ReturnResult>>
    {
        public ReturnQuery Query { get; }

        public SearchReturnsQuery(ReturnQuery query)
        {
            Query = query;
        }
    }
}
