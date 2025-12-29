using Inventory.Dto.Pages.Results;
using Inventory.Dto.PurchasePayments.Results;
using Inventory.Dto.Queries;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.Search
{
    public class SearchPurchasePaymentsQuery : IRequest<PagedResult<PurchasePaymentResult>>
    {
        public PurchasePaymentQuery Query { get; }

        public SearchPurchasePaymentsQuery(PurchasePaymentQuery query)
        {
            Query = query;
        }
    }
}
