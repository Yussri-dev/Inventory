using Inventory.Dto.CashMovements.Results;
using MediatR;

namespace Inventory.Services.Features.CashMovement.Update
{
    public class UpdateCashMovementCommandHandler
    //: IRequestHandler<UpdateCashMovementCommand, CashMovementResult>
    {
        private readonly CashMovementService _cashMovementService;

        public UpdateCashMovementCommandHandler(CashMovementService customerService)
        {
            _cashMovementService = customerService;
            //}

            //public Task<CashMovementResult> Handle(UpdateCashMovementCommand command, CancellationToken cancellationToken)
            //{
            //    return _cashMovementService.UpdateAsync(command.Id, command.Request);
            //}
        }
    }
}
