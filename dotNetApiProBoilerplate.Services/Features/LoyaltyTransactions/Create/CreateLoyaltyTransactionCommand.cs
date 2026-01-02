

using Inventory.Dto.LoyaltyTransactions.Requests;
using Inventory.Dto.LoyaltyTransactions.Results;
using Inventory.Services.Features.LoyaltyTransactions.Create;
using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.Create
{
    public class CreateLoyaltyTransactionCommand : IRequest<LoyaltyTransactionResult>
    {
        public CreateLoyaltyTransactionRequest Request { get; }

        public CreateLoyaltyTransactionCommand(CreateLoyaltyTransactionRequest request)
        {
            Request = request;
        }
    }
}
