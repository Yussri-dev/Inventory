using Inventory.Dto.LoyaltyTransactions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.LoyaltyTransactions.Search;
using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.Search
{
    public class SearchLoyaltyTransactionsQuery : IRequest<PagedResult<LoyaltyTransactionResult>>
    {
        public LoyaltyTransactionQuery Query { get; }

        public SearchLoyaltyTransactionsQuery(LoyaltyTransactionQuery query)
        {
            Query = query;
        }
    }
}
