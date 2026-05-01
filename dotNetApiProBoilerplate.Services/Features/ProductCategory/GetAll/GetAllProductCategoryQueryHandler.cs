using Inventory.Dto.ProductCategory.Results;
using MediatR;


namespace Inventory.Services.Features.ProductCategory.GetAll
{
    public class GetAllProductCategoryQueryHandler
        :IRequestHandler<GetAllProductCategoryQuery, List<ProductCategoryResult>>
    {
        private readonly ProductCategoryService _productCategoryService;
        public GetAllProductCategoryQueryHandler(ProductCategoryService productCategoryService)
        {
            _productCategoryService = productCategoryService;
        }
        public Task<List<ProductCategoryResult>> Handle(GetAllProductCategoryQuery query, CancellationToken cancellationToken)
        {
            return _productCategoryService.GetAllAsync();
        }
    }
}
