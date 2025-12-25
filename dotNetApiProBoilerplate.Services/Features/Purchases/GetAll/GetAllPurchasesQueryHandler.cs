using Inventory.Dto.Purchases.Results;
using MediatR;

namespace Inventory.Services.Features.Purchases.GetAll
{
    public class GetAllPurchasesQueryHandler
        : IRequestHandler<GetAllPurchasesQuery, List<PurchaseResult>>
    {
        private readonly PurchaseService _productService;

        public GetAllPurchasesQueryHandler(PurchaseService productService)
        {
            _productService = productService;
        }

        public Task<List<PurchaseResult>> Handle(GetAllPurchasesQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetAllAsync();
        }
    }
}
