using MediatR;

namespace Inventory.Services.Features.ProductCategory.Delete
{
    public class DeleteProductCategoryCommandHandler 
        : IRequestHandler<DeleteProductCategoryCommand, Unit>
    {
        private readonly ProductCategoryService _productCategoryService;

        public DeleteProductCategoryCommandHandler(ProductCategoryService productCategoryService)
        {
            _productCategoryService = productCategoryService;
        }

        public async Task<Unit> Handle(DeleteProductCategoryCommand command, CancellationToken cancellationToken)
        {
            await _productCategoryService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
