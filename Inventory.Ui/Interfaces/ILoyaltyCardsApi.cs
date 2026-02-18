using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;
using Inventory.Dto.LoyaltyCards.Results;
using Inventory.Dto.LoyaltyCards.Requests;

namespace Inventory.Ui.Interfaces
{
    public interface ILoyaltyCardsApi
    {
        [Post("/api/v1/loyaltyCards")]
        Task<LoyaltyCardResult> Create(CreateLoyaltyCardRequest request);

        [Get("/api/v1/loyaltyCards")]
        Task<List<LoyaltyCardResult>> GetAll();

        [Put("/api/v1/loyaltyCards/{id}")]
        Task<LoyaltyCardResult> Update(Guid id, UpdateLoyaltyCardRequest request);

        [Delete("/api/v1/loyaltyCards/{id}")]
        Task Delete(Guid id);

        Task<PagedResult<LoyaltyCardResult>> Search([Query]  LoyaltyCardQuery query);
    }
}
