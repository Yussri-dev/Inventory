using Inventory.Dto.PurchasePayments.Results;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.GetAll
{
    public class GetAllPurchasePaymentsQueryHandler
       : IRequestHandler<GetAllPurchasePaymentsQuery, List<PurchasePaymentResult>>
    {
        private readonly PurchasePaymentService _productService;

        public GetAllPurchasePaymentsQueryHandler(PurchasePaymentService productService)
        {
            _productService = productService;
        }

        public Task<List<PurchasePaymentResult>> Handle(GetAllPurchasePaymentsQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetAllAsync();
        }
    }

}
