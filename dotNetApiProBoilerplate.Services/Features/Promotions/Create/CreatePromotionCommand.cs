using Inventory.Dto.Promotions.Results;
using Inventory.Dto.Promotions.Requests;
using Inventory.Services.Features.Promotions.Create;
using MediatR;

namespace Inventory.Services.Features.Promotions.Create
{
    public class CreatePromotionCommand : IRequest<PromotionResult>
    {
        public CreatePromotionRequest Request { get; }

        public CreatePromotionCommand(CreatePromotionRequest request)
        {
            Request = request;
        }
    }
}
