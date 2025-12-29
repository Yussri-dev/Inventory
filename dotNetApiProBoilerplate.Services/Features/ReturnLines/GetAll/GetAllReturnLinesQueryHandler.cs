using Inventory.Dto.ReturnLines.Results;
using MediatR;

namespace Inventory.Services.Features.ReturnLines.GetAll
{
    public class GetAllReturnLinesQueryHandler
       : IRequestHandler<GetAllReturnLinesQuery, List<ReturnLineResult>>
    {
        private readonly ReturnLineService _productService;

        public GetAllReturnLinesQueryHandler(ReturnLineService productService)
        {
            _productService = productService;
        }

        public Task<List<ReturnLineResult>> Handle(GetAllReturnLinesQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetAllAsync();
        }
    }
}
