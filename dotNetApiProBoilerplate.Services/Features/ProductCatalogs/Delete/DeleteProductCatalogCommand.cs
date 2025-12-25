using Inventory.Services.Features.ProductCatalogs.Delete;
using MediatR;

namespace Inventory.Services.Features.ProductCatalogs.Delete
{
    public class DeleteProductCatalogCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteProductCatalogCommand(Guid id)
        {
            Id = id;
        }
    }
}
