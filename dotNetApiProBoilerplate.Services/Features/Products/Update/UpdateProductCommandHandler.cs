using Inventory.Dto.Products.Results;
using MediatR;

namespace Inventory.Services.Features.Products.Update
{
    public class UpdateProductCommandHandler
        : IRequestHandler<UpdateProductCommand, ProductResult>
    {
        private readonly ProductService _productService;

        public UpdateProductCommandHandler(ProductService productService)
        {
            _productService = productService;
        }

        public Task<ProductResult> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
        {
            return _productService.UpdateAsync(command.Id, command.Request);
        }
    }
}
