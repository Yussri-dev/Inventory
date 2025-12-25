using Inventory.Dto.CustomerTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.GetById
{
    public class GetCustomerTransactionByIdQuery : IRequest<CustomerTransactionResult>
    {
        public Guid Id { get; }

        public GetCustomerTransactionByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
