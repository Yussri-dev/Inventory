using Inventory.Dto.CashMovements.Requests;
using Inventory.Dto.CashMovements.Results;
using MediatR;

namespace Inventory.Services.Features.CashMovement.Update
{
    public class UpdateCashMovementCommand : IRequest<CashMovementResult>
    {
        public Guid Id { get; }
        public UpdateCashMovementRequest Request { get; }

        public UpdateCashMovementCommand(Guid id, UpdateCashMovementRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
