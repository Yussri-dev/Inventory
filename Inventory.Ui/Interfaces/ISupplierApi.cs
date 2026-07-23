using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Dto.Suppliers.Results;
using Refit;

namespace Inventory.Ui.Interfaces;

public interface ISupplierApi
{
    [Post("/api/v1/suppliers")]
    Task<SupplierResult> Create(
        [Body] CreateSupplierRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/suppliers")]
    Task<List<SupplierResult>> GetAll(
        CancellationToken cancellationToken = default);

    [Get("/api/v1/suppliers/{id}")]
    Task<SupplierResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default);

    [Put("/api/v1/suppliers/{id}")]
    Task<SupplierResult> Update(
        Guid id,
        [Body] UpdateSupplierRequest request,
        CancellationToken cancellationToken = default);

    [Delete("/api/v1/suppliers/{id}")]
    Task Delete(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/suppliers/search")]
    Task<PagedResult<SupplierResult>> Search(
        [Query] SupplierQuery query,
        CancellationToken cancellationToken = default);
}