using Inventory.Dto.CashMovements.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.CashMovement.Search
{
    public class SearchCashMovementsQueryHandler
    : IRequestHandler<SearchCashMovementsQuery, PagedResult<CashMovementResult>>
    {
        private readonly CashMovementService _cashMovementService;

        public SearchCashMovementsQueryHandler(CashMovementService customerService)
        {
            _cashMovementService = customerService;
        }

        public Task<PagedResult<CashMovementResult>> Handle(SearchCashMovementsQuery query, CancellationToken cancellationToken)
        {
            return _cashMovementService.QueryAsync(query.Query);
        }
    }
}
