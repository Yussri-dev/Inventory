
using Inventory.Dto.SaleLines.Results;
using MediatR;

namespace Inventory.Services.Features.SaleLines.GetById
{
    public class GetSaleLineByIdQueryHandler
        : IRequestHandler<GetSaleLineByIdQuery, SaleLineResult>
    {
        private readonly SaleLineService _productService;

        public GetSaleLineByIdQueryHandler(SaleLineService productService)
        {
            _productService = productService;
        }

        public Task<SaleLineResult> Handle(GetSaleLineByIdQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetByIdAsync(query.Id);
        }
    }
}
