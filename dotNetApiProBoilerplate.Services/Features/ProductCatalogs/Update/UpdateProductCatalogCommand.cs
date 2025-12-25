using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Services.Features.ProductCatalogs.Update;
using MediatR;

namespace Inventory.Services.Features.ProductCatalogs.Update
{
    public class UpdateProductCatalogCommand : IRequest<ProductCatalogResult>
    {
        public Guid Id { get; }
        public UpdateProductCatalogRequest Request { get; }

        public UpdateProductCatalogCommand(Guid id, UpdateProductCatalogRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
