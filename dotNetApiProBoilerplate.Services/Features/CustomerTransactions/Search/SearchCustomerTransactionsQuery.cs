using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.Search
{
    public class SearchCustomerTransactionsQuery : IRequest<PagedResult<CustomerTransactionResult>>
    {
        public CustomerTransactionQuery Query { get; }

        public SearchCustomerTransactionsQuery(CustomerTransactionQuery query)
        {
            Query = query;
        }
    }
}
