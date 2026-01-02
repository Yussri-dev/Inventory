using Inventory.Services.Features.SupplierReturns.Delete;
using MediatR;

namespace Inventory.Services.Features.SupplierReturns.Delete
{
    public class DeleteSupplierReturnCommand : IRequest<Unit>
    {
        public Guid Id { get; }
        public DeleteSupplierReturnCommand(Guid id)
        {
            Id = id;
        }
    }
}
