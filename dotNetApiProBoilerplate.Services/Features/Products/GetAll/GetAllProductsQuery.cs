using Inventory.Dto.Products.Results;
using MediatR;

namespace Inventory.Services.Features.Products.GetAll
{
    public class GetAllProductsQuery : IRequest<List<ProductResult>>
    {
    }
}
