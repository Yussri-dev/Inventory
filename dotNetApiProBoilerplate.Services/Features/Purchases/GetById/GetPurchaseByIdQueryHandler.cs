using Inventory.Dto.Purchases.Results;
using MediatR;

namespace Inventory.Services.Features.Purchases.GetById
{
    public class GetPurchaseByIdQueryHandler
        : IRequestHandler<GetPurchaseByIdQuery, PurchaseResult>
    {
        private readonly PurchaseService _productService;

        public GetPurchaseByIdQueryHandler(PurchaseService productService)
        {
            _productService = productService;
        }

        public Task<PurchaseResult> Handle(GetPurchaseByIdQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetByIdAsync(query.Id);
        }
    }
}
