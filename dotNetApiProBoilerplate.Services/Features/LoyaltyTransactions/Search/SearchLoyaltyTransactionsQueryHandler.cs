using Inventory.Dto.LoyaltyTransactions.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.Search
{
    public class SearchLoyaltyTransactionsQueryHandler
    : IRequestHandler<SearchLoyaltyTransactionsQuery, PagedResult<LoyaltyTransactionResult>>
    {
        private readonly LoyaltyTransactionService _customerService;

        public SearchLoyaltyTransactionsQueryHandler(LoyaltyTransactionService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<LoyaltyTransactionResult>> Handle(SearchLoyaltyTransactionsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
