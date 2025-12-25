using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.CustomerTransactions.Requests;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.Create
{
    public class CreateCustomerTransactionCommand : IRequest<CustomerTransactionResult>
    {
        public CreateCustomerTransactionRequest Request { get; }

        public CreateCustomerTransactionCommand(CreateCustomerTransactionRequest request)
        {
            Request = request;
        }
    }
}
