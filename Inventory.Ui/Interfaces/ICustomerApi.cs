using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces;

public interface ICustomerApi
{
    [Get("/api/sync-downloads/customers")]
    Task<Inventory.Dto.Sync.SyncDownload<CustomerResult>> DownloadSnapshot(CancellationToken cancellationToken = default);

    [Post("/api/v1/customers")]
    Task<CustomerResult> Create(
        [Body] CreateCustomerRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/customers")]
    Task<List<CustomerResult>> GetAll(
        CancellationToken cancellationToken = default);

    [Put("/api/v1/customers/{id}")]
    Task<CustomerResult> Update(
        Guid id,
        [Body] UpdateCustomerRequest request,
        CancellationToken cancellationToken = default);

    [Delete("/api/v1/customers/{id}")]
    Task Delete(
        Guid id,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/customers/search")]
    Task<PagedResult<CustomerResult>> Search(
        [Query] CustomerQuery query,
        CancellationToken cancellationToken = default);
}