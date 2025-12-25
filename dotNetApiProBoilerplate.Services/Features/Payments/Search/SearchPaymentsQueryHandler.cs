using Inventory.Dto.Payments.Results;
using Inventory.Dto.Pages.Results;
using MediatR;

namespace Inventory.Services.Features.Payments.Search
{
    public class SearchPaymentsQueryHandler
    : IRequestHandler<SearchPaymentsQuery, PagedResult<PaymentResult>>
    {
        private readonly PaymentService _customerService;

        public SearchPaymentsQueryHandler(PaymentService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<PaymentResult>> Handle(SearchPaymentsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
