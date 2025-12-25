using MediatR;

namespace Inventory.Services.Features.ProductCatalogs.Delete
{
    public class DeleteProductCatalogCommandHandler
        : IRequestHandler<DeleteProductCatalogCommand, Unit>
    {
        private readonly ProductCatalogService _customerService;

        public DeleteProductCatalogCommandHandler(ProductCatalogService customerService)
        {
            _customerService = customerService;
        }

        public async Task<Unit> Handle(DeleteProductCatalogCommand command, CancellationToken cancellationToken)
        {
            await _customerService.DeleteAsync(command.Id);
            return Unit.Value;
        }
    }
}
