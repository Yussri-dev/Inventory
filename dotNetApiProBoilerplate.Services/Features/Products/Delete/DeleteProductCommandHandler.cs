using MediatR;

namespace Inventory.Services.Features.Products.Delete
{
    public class DeleteProductCommandHandler
        : IRequestHandler<DeleteProductCommand, Unit>
    {
        private readonly ProductService _productService;

        public DeleteProductCommandHandler(ProductService productService)
        {
            _productService = productService;
        }

        public async Task<Unit> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
        {
            await _productService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
