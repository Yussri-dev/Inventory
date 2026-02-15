using Inventory.Dto.CustomerTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.CustomerDetails
{
    public record GetCustomerDetailQuery(Guid CustomerId) : IRequest<CustomerDetailResult>;
}
