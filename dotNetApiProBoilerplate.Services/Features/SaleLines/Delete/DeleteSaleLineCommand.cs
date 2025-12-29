

using Inventory.Services.Features.SaleLines.Delete;
using MediatR;

namespace Inventory.Services.Features.SaleLines.Delete
{
    public class DeleteSaleLineCommand : IRequest<Unit>
    {
        public Guid Id { get; }

        public DeleteSaleLineCommand(Guid id)
        {
            Id = id;
        }
    }
}
