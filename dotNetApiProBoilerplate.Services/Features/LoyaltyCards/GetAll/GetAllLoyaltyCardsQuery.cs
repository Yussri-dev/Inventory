using Inventory.Dto.LoyaltyCards.Results;
using MediatR;

namespace Inventory.Services.Features.LoyaltyCards.GetAll
{
    public class GetAllLoyaltyCardsQuery : IRequest<List<LoyaltyCardResult>>
    {
    }
}
