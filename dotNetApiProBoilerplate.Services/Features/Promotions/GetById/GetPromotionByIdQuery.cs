using Inventory.Dto.Promotions.Results;
using Inventory.Services.Features.Promotions.GetById;
using MediatR;


namespace Inventory.Services.Features.Promotions.GetById
{
    public class GetPromotionByIdQuery : IRequest<PromotionResult>
    {
        public Guid Id { get; }

        public GetPromotionByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
