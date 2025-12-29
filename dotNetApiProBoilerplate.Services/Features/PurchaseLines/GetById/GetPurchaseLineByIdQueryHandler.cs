using Inventory.Dto.PurchaseLines.Results;
using MediatR;

namespace Inventory.Services.Features.PurchaseLines.GetById
{
    public class GetPurchaseLineByIdQueryHandler
        : IRequestHandler<GetPurchaseLineByIdQuery, PurchaseLineResult>
    {
        private readonly PurchaseLineService _productService;

        public GetPurchaseLineByIdQueryHandler(PurchaseLineService productService)
        {
            _productService = productService;
        }

        public Task<PurchaseLineResult> Handle(GetPurchaseLineByIdQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetByIdAsync(query.Id);
        }
    }
}
