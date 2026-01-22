using Inventory.Dto.Pages.Results;
using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IPurchaseApi
    {
        [Post("/api/v1/purchases")]
        Task<PurchaseResult> Create(
            [Body] CreatePurchaseRequest request);

        [Post("/api/v1/purchases/complete")]
        Task<PurchaseResult> CreateComplete(
            [Body] CreateCompletePurchaseRequest request);

        [Get("/api/v1/purchases")]
        Task<List<PurchaseResult>> GetAll();

        [Get("/api/v1/purchases/{id}")]
        Task<PurchaseResult> GetById(Guid id);

        [Put("/api/v1/purchases/{id}")]
        Task<PurchaseResult> Update(
            Guid id,
            [Body] UpdatePurchaseRequest request);

        [Delete("/api/v1/purchases/{id}")]
        Task Delete(Guid id);

        [Get("/api/v1/purchases/search")]
        Task<PagedResult<PurchaseResult>> Search(
            [Query] PurchaseQuery query);
    }
}
