using Inventory.Dto.Products.Results;
using MediatR;

namespace Inventory.Services.Features.Products.GetById
{
    public class GetProductByIdQueryHandler
        : IRequestHandler<GetProductByIdQuery, ProductResult>
    {
        private readonly ProductService _productService;

        public GetProductByIdQueryHandler(ProductService productService)
        {
            _productService = productService;
        }

        public Task<ProductResult> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetByIdAsync(query.Id);
        }
    }
}
