using Inventory.Dto.CustomerTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.CustomerDetails
{
    public class GetCustomerDetailQueryHandler
        : IRequestHandler<GetCustomerDetailQuery, CustomerDetailResult>
    {
        private readonly CustomerTransactionService _customerTransactionService;
        public GetCustomerDetailQueryHandler(CustomerTransactionService customerTransactionService)
        {
            _customerTransactionService = customerTransactionService;
        }
        public Task<CustomerDetailResult> Handle(GetCustomerDetailQuery query, CancellationToken cancellationToken)
        {
            return _customerTransactionService.GetCustomerDetailAsync(query.CustomerId);
        }
    }
}
