using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.CashSession.Search
{
    public class SearchCashSessionsQueryHandler
    : IRequestHandler<SearchCashSessionsQuery, PagedResult<CashSessionResult>>
    {
        private readonly CashSessionService _cashSessionService;

        public SearchCashSessionsQueryHandler(CashSessionService customerService)
        {
            _cashSessionService = customerService;
        }

        public Task<PagedResult<CashSessionResult>> Handle(SearchCashSessionsQuery query, CancellationToken cancellationToken)
        {
            return _cashSessionService.QueryAsync(query.Query);
        }
    }
}
