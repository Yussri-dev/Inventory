using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.Search
{
    public class SearchCustomerTransactionsQueryHandler
    : IRequestHandler<SearchCustomerTransactionsQuery, PagedResult<CustomerTransactionResult>>
    {
        private readonly CustomerTransactionService _cashCorrectionService;

        public SearchCustomerTransactionsQueryHandler(CustomerTransactionService customerService)
        {
            _cashCorrectionService = customerService;
        }

        public Task<PagedResult<CustomerTransactionResult>> Handle(SearchCustomerTransactionsQuery query, CancellationToken cancellationToken)
        {
            return _cashCorrectionService.QueryAsync(query.Query);
        }
    }
}
