using Inventory.Dto.LoyaltyTransactions.Results;
using Inventory.Services.Features.LoyaltyTransactions.GetAll;
using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.GetAll
{
    public class GetAllLoyaltyTransactionsQuery : IRequest<List<LoyaltyTransactionResult>>
    {
    }
}
