using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.CustomerTransactions.Requests;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.Update
{
    public class UpdateCustomerTransactionCommand : IRequest<CustomerTransactionResult>
    {
        public Guid Id { get; }
        public UpdateCustomerTransactionRequest Request { get; }

        public UpdateCustomerTransactionCommand(Guid id, UpdateCustomerTransactionRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
