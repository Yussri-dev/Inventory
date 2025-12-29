using Inventory.Dto.Stock.Results;
using Inventory.Services.Features.Stocks.GetAll;
using MediatR;

namespace Inventory.Services.Features.Stocks.GetAll
{
    public class GetAllStocksQuery : IRequest<List<StockResult>>
    {
    }
}
