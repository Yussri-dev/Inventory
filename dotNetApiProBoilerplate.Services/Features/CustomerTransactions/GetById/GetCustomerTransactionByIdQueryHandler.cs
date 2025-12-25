using Inventory.Dto.CustomerTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.GetById
{
    public class GetCustomerTransactionByIdQueryHandler
        : IRequestHandler<GetCustomerTransactionByIdQuery, CustomerTransactionResult>
    {
        private readonly CustomerTransactionService _cashCorrectionService;

        public GetCustomerTransactionByIdQueryHandler(CustomerTransactionService customerService)
        {
            _cashCorrectionService = customerService;
        }

        public Task<CustomerTransactionResult> Handle(GetCustomerTransactionByIdQuery query, CancellationToken cancellationToken)
        {
            return _cashCorrectionService.GetByIdAsync(query.Id);
        }
    }
}
