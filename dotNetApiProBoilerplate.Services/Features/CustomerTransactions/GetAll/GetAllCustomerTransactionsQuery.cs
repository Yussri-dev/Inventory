using Inventory.Dto.CustomerTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.GetAll
{
    public class GetAllCustomerTransactionsQuery : IRequest<List<CustomerTransactionResult>>
    {
    }
}
