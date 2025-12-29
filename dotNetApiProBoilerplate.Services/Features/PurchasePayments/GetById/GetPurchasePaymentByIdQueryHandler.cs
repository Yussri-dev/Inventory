using Inventory.Dto.PurchasePayments.Results;
using MediatR;

namespace Inventory.Services.Features.PurchasePayments.GetById
{
    public class GetPurchasePaymentByIdQueryHandler
        : IRequestHandler<GetPurchasePaymentByIdQuery, PurchasePaymentResult>
    {
        private readonly PurchasePaymentService _productService;

        public GetPurchasePaymentByIdQueryHandler(PurchasePaymentService productService)
        {
            _productService = productService;
        }

        public Task<PurchasePaymentResult> Handle(GetPurchasePaymentByIdQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetByIdAsync(query.Id);
        }
    }
}
