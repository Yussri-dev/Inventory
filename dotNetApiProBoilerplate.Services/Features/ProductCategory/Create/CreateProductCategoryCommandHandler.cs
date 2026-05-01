using Inventory.Dto.ProductCategory.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCategory.Create
{
    public class CreateProductCategoryCommandHandler : IRequestHandler<CreateProductCategoryCommand, ProductCategoryResult>
    {
        private readonly ProductCategoryService _productCategoryService;
        public CreateProductCategoryCommandHandler(ProductCategoryService productCategoryService)
        {
            _productCategoryService = productCategoryService;
        }
        public Task<ProductCategoryResult> Handle(CreateProductCategoryCommand command, CancellationToken cancellationToken)
        {
            return _productCategoryService.CreateAsync(command.Request);
        }
    }
}
