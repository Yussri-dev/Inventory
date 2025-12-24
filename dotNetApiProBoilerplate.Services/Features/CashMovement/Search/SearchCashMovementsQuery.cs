using Inventory.Dto.CashMovements.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.CashMovement.Search;
using MediatR;

namespace Inventory.Services.Features.CashMovement.Search
{
    public class SearchCashMovementsQuery : IRequest<PagedResult<CashMovementResult>>
    {
        public CashMovementQuery Query { get; }

        public SearchCashMovementsQuery(CashMovementQuery query)
        {
            Query = query;
        }
    }
}
