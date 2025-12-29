

using Inventory.Dto.Stock.Results;
using Inventory.Services.Features.Stocks.GetById;
using MediatR;

namespace Inventory.Services.Features.Stocks.GetById
{
    public class GetStockByIdQuery : IRequest<StockResult>
    {
        public Guid Id { get; }

        public GetStockByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
