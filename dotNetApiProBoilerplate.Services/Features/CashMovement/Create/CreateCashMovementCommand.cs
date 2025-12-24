using Inventory.Dto.CashMovements.Results;
using Inventory.Dto.CashMovements.Requests;
using MediatR;

namespace Inventory.Services.Features.CashMovement.Create
{
    public class CreateCashMovementCommand : IRequest<CashMovementResult>
    {
        public CreateCashMovementRequest Request { get; }

        public CreateCashMovementCommand(CreateCashMovementRequest request)
        {
            Request = request;
        }
    }
}
