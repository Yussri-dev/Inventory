using Inventory.Dto.Pages.Results;
using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.GetByCustomer
{
    public class GetSalesByCustomerQueryHandler
        : IRequestHandler<GetSalesByCustomerQuery, PagedResult<SaleResult>>
    {
        private readonly SaleService _saleService;

        public GetSalesByCustomerQueryHandler(SaleService saleService)
        {
            _saleService = saleService;
        }

        public Task<PagedResult<SaleResult>> Handle(
            GetSalesByCustomerQuery request,
            CancellationToken cancellationToken)
        {
            return _saleService.GetByCustomerAsync(request.Query);
        }
    }
}
