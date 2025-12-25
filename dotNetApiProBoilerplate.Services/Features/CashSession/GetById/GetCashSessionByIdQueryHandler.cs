using Inventory.Dto.CashSessions.Results;
using MediatR;

namespace Inventory.Services.Features.CashSession.GetById
{
    public class GetCashSessionByIdQueryHandler
        : IRequestHandler<GetCashSessionByIdQuery, CashSessionResult>
    {
        private readonly CashSessionService _cashSessionService;

        public GetCashSessionByIdQueryHandler(CashSessionService customerService)
        {
            _cashSessionService = customerService;
        }

        public Task<CashSessionResult> Handle(GetCashSessionByIdQuery query, CancellationToken cancellationToken)
        {
            return _cashSessionService.GetByIdAsync(query.Id);
        }
    }
}
