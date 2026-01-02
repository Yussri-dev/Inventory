using Inventory.Dto.Promotions.Results;
using MediatR;


namespace Inventory.Services.Features.Promotions.GetById
{
    public class GetPromotionByIdQueryHandler
        : IRequestHandler<GetPromotionByIdQuery, PromotionResult>
    {
        private readonly PromotionService _customerService;

        public GetPromotionByIdQueryHandler(PromotionService customerService)
        {
            _customerService = customerService;
        }

        public Task<PromotionResult> Handle(GetPromotionByIdQuery query, CancellationToken cancellationToken)
        {
            return _customerService.GetByIdAsync(query.Id);
        }
    }
}
