using Inventory.Dto.ProductCategory.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCategory.GetAll
{
    public class GetAllProductCategoryQuery : IRequest<List<ProductCategoryResult>>
    {
    }
}
