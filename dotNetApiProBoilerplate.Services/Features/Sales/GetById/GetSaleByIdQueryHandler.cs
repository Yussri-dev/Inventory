using Inventory.Dto.Sales.Results;
using MediatR;

namespace Inventory.Services.Features.Sales.GetById
{
    public class GetSaleByIdQueryHandler
        : IRequestHandler<GetSaleByIdQuery, SaleResult>
    {
        private readonly SaleService _productService;

        public GetSaleByIdQueryHandler(SaleService productService)
        {
            _productService = productService;
        }

        public Task<SaleResult> Handle(GetSaleByIdQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetByIdAsync(query.Id);
        }
    }
}
