using Inventory.Dto.Products.Results;
using Inventory.Services.Abstractions;
using MediatR;
namespace Inventory.Services.Features.Products.RequestCatalogApproval
{
    public sealed class RequestCatalogApprovalCommandHandler
    : IRequestHandler<RequestCatalogApprovalCommand, ProductResult>
    {
        private readonly ProductService _productService;

        public RequestCatalogApprovalCommandHandler(
            ProductService productService)
        {
            _productService = productService;
        }

        public async Task<ProductResult> Handle(
            RequestCatalogApprovalCommand request,
            CancellationToken cancellationToken)
        {
            return await _productService
                .RequestCatalogApprovalAsync(request.ProductId);
        }
    }
}
