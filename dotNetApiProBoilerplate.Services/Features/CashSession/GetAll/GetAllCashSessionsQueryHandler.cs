using Inventory.Dto.CashSessions.Results;
using MediatR;

namespace Inventory.Services.Features.CashSession.GetAll
{
    public class GetAllCashSessionsQueryHandler
        : IRequestHandler<GetAllCashSessionsQuery, List<CashSessionResult>>
    {
        private readonly CashSessionService _cashSessionService;

        public GetAllCashSessionsQueryHandler(CashSessionService cashSessionService)
        {
            _cashSessionService = cashSessionService;
        }

        public Task<List<CashSessionResult>> Handle(GetAllCashSessionsQuery query, CancellationToken cancellationToken)
        {
            return _cashSessionService.GetAllAsync();
        }
    }
}
