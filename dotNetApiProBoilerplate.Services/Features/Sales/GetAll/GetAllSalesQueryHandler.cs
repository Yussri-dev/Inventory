using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.GetAll
{
    public class GetAllSalesQueryHandler
        : IRequestHandler<GetAllSalesQuery, List<SaleResult>>
    {
        private readonly SaleService _productService;

        public GetAllSalesQueryHandler(SaleService productService)
        {
            _productService = productService;
        }

        public Task<List<SaleResult>> Handle(GetAllSalesQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetAllAsync();
        }
    }
}
