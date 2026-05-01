using Inventory.Dto.ProductCategory.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCategory.Update
{
    public class UpdateProductCategoryCommandHandler
        : IRequestHandler<UpdateProductCategoryCommand, ProductCategoryResult>
    {
        private readonly ProductCategoryService _productCategoryService;
        public UpdateProductCategoryCommandHandler(ProductCategoryService productCategoryService)
        {
            _productCategoryService = productCategoryService;
        }
        public Task<ProductCategoryResult> Handle(UpdateProductCategoryCommand command, CancellationToken cancellationToken)
        {
            return _productCategoryService.UpdateAsync(command.Id, command.Request);
        }
    }
}
