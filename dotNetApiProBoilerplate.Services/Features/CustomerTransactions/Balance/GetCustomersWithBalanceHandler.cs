using Inventory.Dto.Queries;
using MediatR;


namespace Inventory.Services.Features.CustomerTransactions.Balance
{
    public class GetCustomersWithBalanceHandler : IRequestHandler<GetCustomersWithBalanceQuery, List<CustomerCreditResult>>
    {
        private readonly CustomerTransactionService _service;

        public GetCustomersWithBalanceHandler(CustomerTransactionService service)
        {
            _service = service;
        }

        public async Task<List<CustomerCreditResult>> Handle(
            GetCustomersWithBalanceQuery request,
            CancellationToken cancellationToken)
        {
            return await _service.GetCustomersWithBalanceAsync();
        }
    }
}
