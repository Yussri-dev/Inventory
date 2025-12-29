using Inventory.Dto.ReturnLines.Results;
using MediatR;

namespace Inventory.Services.Features.ReturnLines.GetById
{
    public class GetReturnLineByIdQueryHandler
        : IRequestHandler<GetReturnLineByIdQuery, ReturnLineResult>
    {
        private readonly ReturnLineService _productService;

        public GetReturnLineByIdQueryHandler(ReturnLineService productService)
        {
            _productService = productService;
        }

        public Task<ReturnLineResult> Handle(GetReturnLineByIdQuery query, CancellationToken cancellationToken)
        {
            return _productService.GetByIdAsync(query.Id);
        }
    }
}
