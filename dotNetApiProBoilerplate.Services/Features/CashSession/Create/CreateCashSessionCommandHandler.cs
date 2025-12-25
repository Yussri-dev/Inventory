using Inventory.Dto.CashSessions.Results;
using MediatR;

namespace Inventory.Services.Features.CashSession.Create
{
    public class CreateCashSessionCommandHandler : IRequestHandler<CreateCashSessionCommand, CashSessionResult>
    {
        private readonly CashSessionService _cashSessionService;

        public CreateCashSessionCommandHandler(CashSessionService productService)
        {
            _cashSessionService = productService;
        }

        public Task<CashSessionResult> Handle(CreateCashSessionCommand command, CancellationToken cancellationToken)
        {
            return _cashSessionService.CreateAsync(command.Request);
        }
    }
}
