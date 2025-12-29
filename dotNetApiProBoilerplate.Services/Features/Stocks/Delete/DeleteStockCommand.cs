using Inventory.Services.Features.Stocks.Delete;
using MediatR;

namespace Inventory.Services.Features.Stocks.Delete
{
    public class DeleteStockCommand : IRequest<Unit>
    {
        public Guid Id { get; }

        public DeleteStockCommand(Guid id)
        {
            Id = id;
        }
    }
}
