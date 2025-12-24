using Inventory.Dto.Products.Results;
using MediatR;

namespace Inventory.Services.Features.Products.GetById
{
    public class GetProductByIdQuery : IRequest<ProductResult>
    {
        public Guid Id { get; }

        public GetProductByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
