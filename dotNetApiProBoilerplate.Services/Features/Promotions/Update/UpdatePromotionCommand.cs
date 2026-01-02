using Inventory.Dto.Promotions.Results;
using Inventory.Dto.Promotions.Requests;
using Inventory.Services.Features.Promotions.Update;
using MediatR;

namespace Inventory.Services.Features.Promotions.Update
{
    public class UpdatePromotionCommand : IRequest<PromotionResult>
    {
        public Guid Id { get; }
        public UpdatePromotionRequest Request { get; }

        public UpdatePromotionCommand(Guid id, UpdatePromotionRequest request)
        {
            Id = id;
            Request = request;
        }
    }
}
