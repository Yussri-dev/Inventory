using Inventory.Dto.GlobalRequests.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Dto.Suppliers.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface ISupplierApi
    {
        [Post("/api/v1/suppliers")]
        Task<SupplierResult> Create(CreateSupplierRequest request);

        [Get("/api/v1/suppliers")]
        Task<List<SupplierResult>> GetAll();

        [Put("/api/v1/suppliers/{id}")]
        Task<SupplierResult> Update(Guid id, UpdateSupplierRequest request);

        [Delete("/api/v1/suppliers/{id}")]
        Task Delete(Guid id);

        [Get("/api/v1/suppliers/search")]
        Task<PagedResult<SupplierResult>> Search([Query] SupplierQuery query);
    }

}
