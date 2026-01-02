using Inventory.Dto.Pages.Results;
using Inventory.Dto.Promotions.Results;
using Inventory.Dto.Queries;
using Inventory.Services.Features.Promotions.Search;
using MediatR;

namespace Inventory.Services.Features.Promotions.Search
{
    public class SearchPromotionsQuery : IRequest<PagedResult<PromotionResult>>
    {
        public PromotionQuery Query { get; }

        public SearchPromotionsQuery(PromotionQuery query)
        {
            Query = query;
        }
    }
}
