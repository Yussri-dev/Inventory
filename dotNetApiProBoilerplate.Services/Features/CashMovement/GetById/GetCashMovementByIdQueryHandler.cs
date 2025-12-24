using Inventory.Dto.CashMovements.Results;
using MediatR;

namespace Inventory.Services.Features.CashMovement.GetById
{
    public class GetCashMovementByIdQueryHandler
        : IRequestHandler<GetCashMovementByIdQuery, CashMovementResult>
    {
        private readonly CashMovementService _cashMovementService;

        public GetCashMovementByIdQueryHandler(CashMovementService customerService)
        {
            _cashMovementService = customerService;
        }

        public Task<CashMovementResult> Handle(GetCashMovementByIdQuery query, CancellationToken cancellationToken)
        {
            return _cashMovementService.GetByIdAsync(query.Id);
        }
    }
}
