using Inventory.Dto.ProductCategory.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCategory.GetById
{
    public class GetProductCategoryByIdQueryHandler
        : IRequestHandler<GetProductCategoryByIdQuery, ProductCategoryResult>
    {
        private readonly ProductCategoryService _productCategoryService;

        public GetProductCategoryByIdQueryHandler(ProductCategoryService productCategoryService)
        {
            _productCategoryService = productCategoryService;
        }
        
        public Task<ProductCategoryResult> Handle(GetProductCategoryByIdQuery query, CancellationToken cancellationToken)
        {
            return _productCategoryService.GetByIdAsync(query.Id);
        }
    }
}
