using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Customers.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalCustomerService
    {
        Task<CustomerResult?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<CustomerResult> CreateAsync(
            CreateCustomerRequest request,
            CancellationToken cancellationToken = default);

        Task<CustomerResult> UpdateAsync(
            Guid id,
            UpdateCustomerRequest request,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<PagedResult<CustomerResult>> QueryAsync(
            CustomerQuery query,
            CancellationToken cancellationToken = default);

        Task<List<CustomerResult>> GetAllAsync(
            CancellationToken cancellationToken = default);
    }
}
