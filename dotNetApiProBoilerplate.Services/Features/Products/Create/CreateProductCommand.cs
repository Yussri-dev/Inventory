using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;
using MediatR;

namespace Inventory.Services.Features.Products.Create
{
    public class CreateProductCommand : IRequest<ProductResult>
    {
        public CreateProductRequest Request { get; }

        public CreateProductCommand(CreateProductRequest request)
        {
            Request = request;
        }
    }
}
