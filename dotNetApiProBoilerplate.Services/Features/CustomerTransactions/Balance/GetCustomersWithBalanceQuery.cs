using Inventory.Dto.Queries;
using MediatR;


namespace Inventory.Services.Features.CustomerTransactions.Balance
{
    public record GetCustomersWithBalanceQuery : IRequest<List<CustomerCreditResult>>;
}
