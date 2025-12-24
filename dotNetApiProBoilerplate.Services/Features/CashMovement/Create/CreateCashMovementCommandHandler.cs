using Inventory.Dto.CashMovements.Results;
using MediatR;

namespace Inventory.Services.Features.CashMovement.Create
{
    public class CreateCashMovementCommandHandler : IRequestHandler<CreateCashMovementCommand, CashMovementResult>
    {
        private readonly CashMovementService _cashMovementService;

        public CreateCashMovementCommandHandler(CashMovementService productService)
        {
            _cashMovementService = productService;
        }

        public Task<CashMovementResult> Handle(CreateCashMovementCommand command, CancellationToken cancellationToken)
        {
            return _cashMovementService.CreateAsync(command.Request);
        }
    }
}
