using Inventory.Dto.CashMovements.Results;
using Inventory.Services.Features.CashMovement.GetAll;
using MediatR;

namespace Inventory.Services.Features.CashMovement.GetAll
{
    public class GetAllCashMovementsQuery : IRequest<List<CashMovementResult>>
    {
    }
}
