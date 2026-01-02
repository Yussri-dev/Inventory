using Inventory.Dto.LoyaltyTransactions.Results;
using Inventory.Dto.LoyaltyTransactions.Requests;
using MediatR;

namespace Inventory.Services.Features.LoyaltyTransactions.Update
{
    public class UpdateLoyaltyTransactionCommand : IRequest<LoyaltyTransactionResult>
    {
        public Guid Id { get; }
        public UpdateLoyaltyTransactionRequest Request { get; }

        public UpdateLoyaltyTransactionCommand(Guid id, UpdateLoyaltyTransactionRequest request)
        {
            Id = id;
            Request = request;
        }
    }

    //public class UpdateLoyaltyTransactionCommandHandler
    //   : IRequestHandler<UpdateLoyaltyTransactionCommand, LoyaltyTransactionResult>
    //{
    //    private readonly LoyaltyTransactionService _customerService;

    //    public UpdateLoyaltyTransactionCommandHandler(LoyaltyTransactionService customerService)
    //    {
    //        _customerService = customerService;
    //    }

    //    public Task<LoyaltyTransactionResult> Handle(UpdateLoyaltyTransactionCommand command, CancellationToken cancellationToken)
    //    {
    //        return _customerService.UpdateAsync(command.Id, command.Request);
    //    }
    //}
}
