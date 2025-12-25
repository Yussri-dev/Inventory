using Inventory.Dto.ProductCatalogs.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCatalogs.Update
{
    public class UpdateProductCatalogCommandHandler
       : IRequestHandler<UpdateProductCatalogCommand, ProductCatalogResult>
    {
        private readonly ProductCatalogService _customerService;

        public UpdateProductCatalogCommandHandler(ProductCatalogService customerService)
        {
            _customerService = customerService;
        }

        public Task<ProductCatalogResult> Handle(UpdateProductCatalogCommand command, CancellationToken cancellationToken)
        {
            return _customerService.UpdateAsync(command.Id, command.Request);
        }
    }
}
