using Inventory.Dto.Damages.Requests;
using Inventory.Dto.Damages.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces;

public interface IDamageApi
{
    [Post("/api/v1.0/damages")]
    Task<DamageResult> Create(
        [Body] CreateDamageRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/v1.0/damages/complete")]
    Task<DamageResult> Complete(
        [Body] CompleteDamageRequest request,
        CancellationToken cancellationToken = default);

    [Post("/api/v1.0/damages/validate")]
    Task ValidateAll(
        CancellationToken cancellationToken = default);

    [Get("/api/v1.0/damages/{id}")]
    Task<DamageResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/v1.0/damages")]
    Task<List<DamageResult>> GetAll(
        CancellationToken cancellationToken = default);

    [Get("/api/v1.0/damages/search")]
    Task<PagedResult<DamageResult>> Search(
        [Query] DamageQuery query,
        CancellationToken cancellationToken = default);

    [Put("/api/v1.0/damages/{id}")]
    Task<DamageResult> Update(
        Guid id,
        [Body] UpdateDamageRequest request,
        CancellationToken cancellationToken = default);

    [Delete("/api/v1.0/damages/{id}")]
    Task Delete(
        Guid id,
        CancellationToken cancellationToken = default);
}