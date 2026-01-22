using Inventory.Dto.CashSessions.Results;
using MediatR;

namespace Inventory.Services.Features.CashSession.Close
{
    public class CloseCashSessionCommandHandler
        : IRequestHandler<CloseCashSessionCommand, CashSessionResult>
    {
        private readonly CashSessionService _service;

        public CloseCashSessionCommandHandler(CashSessionService service)
        {
            _service = service;
        }

        public async Task<CashSessionResult> Handle(
            CloseCashSessionCommand command,
            CancellationToken cancellationToken)
        {
            return await _service.CloseSessionAsync(
                command.Id,
                command.Request
            );
        }
    }
}
