using Inventory.Dto.LoyaltyTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.GetAll
{
    public class GetAllLoyaltyTransactionsQueryHandler
   : IRequestHandler<GetAllLoyaltyTransactionsQuery, List<LoyaltyTransactionResult>>
    {
        private readonly LoyaltyTransactionService _customerService;

        public GetAllLoyaltyTransactionsQueryHandler(LoyaltyTransactionService customerService)
        {
            _customerService = customerService;
        }

        public Task<List<LoyaltyTransactionResult>> Handle(GetAllLoyaltyTransactionsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetAllAsync();
        }
    }
}
