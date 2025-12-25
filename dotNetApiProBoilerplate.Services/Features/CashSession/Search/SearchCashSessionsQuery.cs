using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.CashSession.Search;
using MediatR;

namespace Inventory.Services.Features.CashSession.Search
{
    public class SearchCashSessionsQuery : IRequest<PagedResult<CashSessionResult>>
    {
        public CashSessionQuery Query { get; }

        public SearchCashSessionsQuery(CashSessionQuery query)
        {
            Query = query;
        }
    }
}
