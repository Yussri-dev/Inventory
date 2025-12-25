using Inventory.Dto.CashSessions.Results;
using MediatR;

namespace Inventory.Services.Features.CashSession.Update
{
    public class UpdateCashSessionCommandHandler
       : IRequestHandler<UpdateCashSessionCommand, CashSessionResult>
    {
        private readonly CashSessionService _cashSessionService;

        public UpdateCashSessionCommandHandler(CashSessionService customerService)
        {
            _cashSessionService = customerService;
        }

        public Task<CashSessionResult> Handle(UpdateCashSessionCommand command, CancellationToken cancellationToken)
        {
            return _cashSessionService.UpdateAsync(command.Id, command.Request);
        }
    }
}
