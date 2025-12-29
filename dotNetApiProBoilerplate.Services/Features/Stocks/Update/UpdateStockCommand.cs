using Inventory.Dto.Stock.Requests;
using Inventory.Dto.Stock.Results;
using Inventory.Services.Features.Stocks.Update;
using MediatR;


namespace Inventory.Services.Features.Stocks.Update
{
    public class UpdateStockCommand : IRequest<StockResult>
    {
        public Guid Id { get; }
        public UpdateStockRequest Request { get; }

        public UpdateStockCommand(Guid id, UpdateStockRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
