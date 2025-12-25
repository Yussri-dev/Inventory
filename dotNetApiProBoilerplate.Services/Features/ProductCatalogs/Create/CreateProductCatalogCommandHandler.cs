using Inventory.Dto.ProductCatalogs.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCatalogs.Create
{
    public class CreateProductCatalogCommandHandler : IRequestHandler<CreateProductCatalogCommand, ProductCatalogResult>
    {
        private readonly ProductCatalogService _customerService;

        public CreateProductCatalogCommandHandler(ProductCatalogService productService)
        {
            _customerService = productService;
        }

        public Task<ProductCatalogResult> Handle(CreateProductCatalogCommand command, CancellationToken cancellationToken)
        {
            return _customerService.CreateAsync(command.Request);
        }
    }
}
