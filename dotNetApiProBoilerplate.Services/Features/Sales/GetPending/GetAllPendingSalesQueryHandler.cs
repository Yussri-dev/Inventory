using Inventory.Dto.Sales.Results;
using MediatR;


namespace Inventory.Services.Features.Sales.GetPending
{
    public class GetAllPendingSalesQueryHandler
        :IRequestHandler<GetAllPendingSalesQuery, List<SaleResult>>
    {
        private readonly SaleService _saleService;

        public GetAllPendingSalesQueryHandler(SaleService saleService)
        {
            _saleService = saleService;
        }

        public Task<List<SaleResult>>Handle(GetAllPendingSalesQuery query, CancellationToken cancellationToken)
        {
            return _saleService.GetPendingAsync();
        }
       
    }
}
