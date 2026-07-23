using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Dto.Suppliers.Results;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalSupplierService
    {
        Task<SupplierResult> CreateAsync(
            CreateSupplierRequest request,
            CancellationToken cancellationToken = default);

        Task<SupplierResult> UpdateAsync(
            Guid id,
            UpdateSupplierRequest request,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<PagedResult<SupplierResult>> QueryAsync(
            SupplierQuery query,
            CancellationToken cancellationToken = default);

        Task<List<SupplierResult>> GetAllAsync(
            CancellationToken cancellationToken = default);
    }
}
