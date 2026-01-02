using Inventory.Dto.Pages.Results;
using Inventory.Dto.Promotions.Results;
using MediatR;

namespace Inventory.Services.Features.Promotions.Search
{
    public class SearchPromotionsQueryHandler
    : IRequestHandler<SearchPromotionsQuery, PagedResult<PromotionResult>>
    {
        private readonly PromotionService _customerService;

        public SearchPromotionsQueryHandler(PromotionService customerService)
        {
            _customerService = customerService;
        }

        public Task<PagedResult<PromotionResult>> Handle(SearchPromotionsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.QueryAsync(query.Query);
        }
    }
}
