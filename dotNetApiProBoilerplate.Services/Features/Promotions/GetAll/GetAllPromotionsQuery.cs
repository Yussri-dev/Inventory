using Inventory.Dto.Promotions.Results;
using MediatR;

namespace Inventory.Services.Features.Promotions.GetAll
{
    public class GetAllPromotionsQuery : IRequest<List<PromotionResult>>
    {
    }
}

