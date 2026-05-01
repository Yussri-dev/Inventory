using Inventory.Dto.ProductCategory.Results;
using MediatR;


namespace Inventory.Services.Features.ProductCategory.GetById
{
    public class GetProductCategoryByIdQuery : IRequest<ProductCategoryResult>
    {
        public Guid Id { get; }

        public GetProductCategoryByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
