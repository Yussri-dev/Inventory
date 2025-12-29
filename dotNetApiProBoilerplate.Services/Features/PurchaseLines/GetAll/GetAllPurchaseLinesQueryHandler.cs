using Inventory.Dto.PurchaseLines.Results;
using MediatR;


namespace Inventory.Services.Features.PurchaseLines.GetAll
{
    public class GetAllPurchaseLinesQueryHandler
       : IRequestHandler<GetAllPurchaseLinesQuery, List<PurchaseLineResult>>
    {
        private readonly PurchaseLineService _productService;

        public GetAllPurchaseLinesQueryHandler(PurchaseLineService productService)
        {
            _productService = productService;
        }

        public Task<List<PurchaseLineResult>> Handle(GetAllPurchaseLinesQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetAllAsync();
        }
    }
}
