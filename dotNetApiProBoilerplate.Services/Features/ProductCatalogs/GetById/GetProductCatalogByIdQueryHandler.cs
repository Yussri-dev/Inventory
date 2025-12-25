using Inventory.Dto.ProductCatalogs.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCatalogs.GetById
{
    public class GetProductCatalogByIdQueryHandler
        : IRequestHandler<GetProductCatalogByIdQuery, ProductCatalogResult>
    {
        private readonly ProductCatalogService _customerService;

        public GetProductCatalogByIdQueryHandler(ProductCatalogService customerService)
        {
            _customerService = customerService;
        }

        public Task<ProductCatalogResult> Handle(GetProductCatalogByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
