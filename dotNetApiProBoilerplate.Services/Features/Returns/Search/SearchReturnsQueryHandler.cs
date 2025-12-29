using Inventory.Dto.Pages.Results;
using Inventory.Dto.Returns.Results;
using MediatR;

namespace Inventory.Services.Features.Returns.Search
{
    public class SearchReturnsQueryHandler
    : IRequestHandler<SearchReturnsQuery, PagedResult<ReturnResult>>
    {
        private readonly ReturnService _productService;

        public SearchReturnsQueryHandler(ReturnService productService)
        {
            _productService = productService;
        }

        public Task<PagedResult<ReturnResult>> Handle(SearchReturnsQuery query, CancellationToken cancellationToken)
        {
            return _productService.QueryAsync(query.Query);
        }
    }
}
