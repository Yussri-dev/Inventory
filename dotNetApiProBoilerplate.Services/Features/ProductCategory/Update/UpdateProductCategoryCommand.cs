
using Inventory.Dto.ProductCategory.Requests;
using Inventory.Dto.ProductCategory.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCategory.Update
{
    public class UpdateProductCategoryCommand : IRequest<ProductCategoryResult>
    {
        public Guid Id { get; }

        public UpdateProductCategoryRequest Request { get; }

        public UpdateProductCategoryCommand(Guid id, UpdateProductCategoryRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
