using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;
using MediatR;

namespace Inventory.Services.Features.Products.Update
{
    public class UpdateProductCommand : IRequest<ProductResult>
    {
        public Guid Id { get; }
        public UpdateProductRequest Request { get; }

        public UpdateProductCommand(Guid id, UpdateProductRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
