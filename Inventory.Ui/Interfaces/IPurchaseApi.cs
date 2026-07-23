using Inventory.Dto.Pages.Results;
using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces;

public interface IPurchaseApi
{
    [Post("/api/v1/purchases")]
    Task<PurchaseResult> Create(
        [Body] CreatePurchaseRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/v1/purchases/complete")]
    Task<PurchaseResult> CreateComplete(
        [Body] CreateCompletePurchaseRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/purchases")]
    Task<List<PurchaseResult>> GetAll(
        CancellationToken cancellationToken = default);

    [Get("/api/v1/purchases/{id}")]
    Task<PurchaseResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default);

    [Put("/api/v1/purchases/{id}")]
    Task<PurchaseResult> Update(
        Guid id,
        [Body] UpdatePurchaseRequest request,
        CancellationToken cancellationToken = default);

    [Delete("/api/v1/purchases/{id}")]
    Task Delete(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/purchases/search")]
    Task<PagedResult<PurchaseResult>> Search(
        [Query] PurchaseQuery query,
        CancellationToken cancellationToken = default);
}