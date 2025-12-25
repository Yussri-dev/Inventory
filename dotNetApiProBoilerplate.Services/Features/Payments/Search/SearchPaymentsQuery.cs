using Inventory.Dto.Payments.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.Payments.Search;
using MediatR;

namespace Inventory.Services.Features.Payments.Search
{
    public class SearchPaymentsQuery : IRequest<PagedResult<PaymentResult>>
    {
        public PaymentQuery Query { get; }

        public SearchPaymentsQuery(PaymentQuery query)
        {
            Query = query;
        }
    }
}
