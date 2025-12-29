using Inventory.Dto.Stock.Requests;
using Inventory.Dto.Stock.Results;
using Inventory.Services.Features.Stocks.Create;
using MediatR;

namespace Inventory.Services.Features.Stocks.Create
{
    public class CreateStockCommand : IRequest<StockResult>
    {
        public CreateStockRequest Request { get; }

        public CreateStockCommand(CreateStockRequest request)
        {
            Request = request;
        }
    }
}
