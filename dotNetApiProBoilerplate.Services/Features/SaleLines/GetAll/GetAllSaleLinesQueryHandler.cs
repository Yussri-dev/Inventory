

using Inventory.Dto.SaleLines.Results;
using MediatR;

namespace Inventory.Services.Features.SaleLines.GetAll
{
    public class GetAllSaleLinesQueryHandler
       : IRequestHandler<GetAllSaleLinesQuery, List<SaleLineResult>>
    {
        private readonly SaleLineService _productService;

        public GetAllSaleLinesQueryHandler(SaleLineService productService)
        {
            _productService = productService;
        }

        public Task<List<SaleLineResult>> Handle(GetAllSaleLinesQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetAllAsync();
        }
    }
}
