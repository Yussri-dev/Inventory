using Inventory.Dto.Pages.Results;
using Inventory.Dto.PurchasePayments.Results;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.Search
{
    public class SearchPurchasePaymentsQueryHandler
    : IRequestHandler<SearchPurchasePaymentsQuery, PagedResult<PurchasePaymentResult>>
    {
        private readonly PurchasePaymentService _productService;

        public SearchPurchasePaymentsQueryHandler(PurchasePaymentService productService)
        {
            _productService = productService;
        }

        public Task<PagedResult<PurchasePaymentResult>> Handle(SearchPurchasePaymentsQuery query, CancellationToken cancellationToken)
        {
            return _productService.QueryAsync(query.Query);
        }
    }
}
