using Inventory.Dto.ProductCatalogs.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCatalogs.GetAll
{
    public class GetAllProductCatalogsQuery : IRequest<List<ProductCatalogResult>>
    {
    }
}
