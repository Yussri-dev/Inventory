using Inventory.Dto.ProductCategory.Requests;
using Inventory.Dto.ProductCategory.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCategory.Create
{
    public class CreateProductCategoryCommand : IRequest<ProductCategoryResult>
    {
        public CreateProductCategoryRequest Request { get; }
        public CreateProductCategoryCommand(CreateProductCategoryRequest request)
        {
            Request = request;
        }
    }
}
