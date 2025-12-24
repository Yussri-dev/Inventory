using Inventory.Dto.CashMovements.Results;
using Inventory.Services.Features.CashMovement.GetById;
using MediatR;

namespace Inventory.Services.Features.CashMovement.GetById
{
    public class GetCashMovementByIdQuery : IRequest<CashMovementResult>
    {
        public Guid Id { get; }

        public GetCashMovementByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
