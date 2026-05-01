using Inventory.Dto.ProductCatalogs.Results;
using MediatR;

namespace Inventory.Services.Features.ProductCatalogs.GetById
{
    public class GetProductCatalogByIdQuery : IRequest<ProductCatalogResult>
    {
        public Guid Id { get; }

        public GetProductCatalogByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
