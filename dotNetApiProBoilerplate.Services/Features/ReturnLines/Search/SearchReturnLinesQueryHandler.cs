using Inventory.Dto.Pages.Results;
using Inventory.Dto.ReturnLines.Results;
using MediatR;

namespace Inventory.Services.Features.ReturnLines.Search
{
    public class SearchReturnLinesQueryHandler
    : IRequestHandler<SearchReturnLinesQuery, PagedResult<ReturnLineResult>>
    {
        private readonly ReturnLineService _productService;

        public SearchReturnLinesQueryHandler(ReturnLineService productService)
        {
            _productService = productService;
        }

        public Task<PagedResult<ReturnLineResult>> Handle(SearchReturnLinesQuery query, CancellationToken cancellationToken)
        {
            return _productService.QueryAsync(query.Query);
        }
    }
}
