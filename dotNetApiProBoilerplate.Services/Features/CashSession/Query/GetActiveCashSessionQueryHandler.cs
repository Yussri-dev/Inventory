using Inventory.Dto.CashSessions.Results;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Services.Features.CashSession.Query
{
    public class GetActiveCashSessionQueryHandler
    : IRequestHandler<GetActiveCashSessionQuery, CashSessionResult>
    {
        private readonly CashSessionService _service;

        public GetActiveCashSessionQueryHandler(CashSessionService service)
        {
            _service = service;
        }

        public async Task<CashSessionResult> Handle(
            GetActiveCashSessionQuery request,
            CancellationToken cancellationToken)
        {
            return await _service.GetActiveAsync();
        }
    }
}
