using Inventory.Dto.CashMovements.Results;
using MediatR;

namespace Inventory.Services.Features.CashMovement.GetAll
{
    public class GetAllCashMovementsQueryHandler
        : IRequestHandler<GetAllCashMovementsQuery, List<CashMovementResult>>
    {
        private readonly CashMovementService _cashMovementService;

        public GetAllCashMovementsQueryHandler(CashMovementService cashMovementService)
        {
            _cashMovementService = cashMovementService;
        }

        public Task<List<CashMovementResult>> Handle(GetAllCashMovementsQuery query, CancellationToken cancellationToken)
        {
            return _cashMovementService.GetAllAsync();
        }
    }
}
