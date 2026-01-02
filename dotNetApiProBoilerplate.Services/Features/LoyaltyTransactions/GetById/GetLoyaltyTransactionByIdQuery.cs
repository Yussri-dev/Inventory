using Inventory.Dto.LoyaltyTransactions.Results;
using Inventory.Services.Features.LoyaltyTransactions.GetById;
using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.GetById
{
    public class GetLoyaltyTransactionByIdQuery : IRequest<LoyaltyTransactionResult>
    {
        public Guid Id { get; }

        public GetLoyaltyTransactionByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
