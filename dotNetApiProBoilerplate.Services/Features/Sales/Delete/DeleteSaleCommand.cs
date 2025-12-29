
using Inventory.Services.Features.Sales.Delete;
using MediatR;

namespace Inventory.Services.Features.Sales.Delete
{
    public class DeleteSaleCommand : IRequest<Unit>
    {
        public Guid Id { get; }

        public DeleteSaleCommand(Guid id)
        {
            Id = id;
        }
    }
}
