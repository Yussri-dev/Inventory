using Inventory.Dto.LoyaltyTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.GetById
{
    public class GetLoyaltyTransactionByIdQueryHandler
       : IRequestHandler<GetLoyaltyTransactionByIdQuery, LoyaltyTransactionResult>
    {
        private readonly LoyaltyTransactionService _customerService;

        public GetLoyaltyTransactionByIdQueryHandler(LoyaltyTransactionService customerService)
        {
            _customerService = customerService;
        }

        public Task<LoyaltyTransactionResult> Handle(GetLoyaltyTransactionByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
