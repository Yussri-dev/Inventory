using Inventory.Dto.Promotions.Results;
using MediatR;

namespace Inventory.Services.Features.Promotions.GetAll
{
    public class GetAllPromotionsQueryHandler
    : IRequestHandler<GetAllPromotionsQuery, List<PromotionResult>>
    {
        private readonly PromotionService _customerService;

        public GetAllPromotionsQueryHandler(PromotionService customerService)
        {
            _customerService = customerService;
        }

        public Task<List<PromotionResult>> Handle(GetAllPromotionsQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetAllAsync();
        }
    }
}

