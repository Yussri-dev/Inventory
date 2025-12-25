using Inventory.Dto.CustomerTransactions.Results;
using MediatR;

namespace Inventory.Services.Features.CustomerTransactions.GetAll
{
    public class GetAllCustomerTransactionsQueryHandler
        : IRequestHandler<GetAllCustomerTransactionsQuery, List<CustomerTransactionResult>>
    {
        private readonly CustomerTransactionService _cashCorrectionService;

        public GetAllCustomerTransactionsQueryHandler(CustomerTransactionService cashCorrectionService)
        {
            _cashCorrectionService = cashCorrectionService;
        }

        public Task<List<CustomerTransactionResult>> Handle(GetAllCustomerTransactionsQuery query, CancellationToken cancellationToken)
        {
            return _cashCorrectionService.GetAllAsync();
        }
    }
}
