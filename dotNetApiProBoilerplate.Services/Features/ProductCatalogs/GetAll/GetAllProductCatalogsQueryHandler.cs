using Inventory.Dto.ProductCatalogs.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCatalogs.GetAll
{
    public class GetAllProductCatalogsQueryHandler
        : IRequestHandler<GetAllProductCatalogsQuery, List<ProductCatalogResult>>
    {
        private readonly ProductCatalogService _customerService;

        public GetAllProductCatalogsQueryHandler(ProductCatalogService customerService)
        {
            _customerService = customerService;
        }

        public Task<List<ProductCatalogResult>> Handle(GetAllProductCatalogsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetAllAsync();
        }
    }
}
