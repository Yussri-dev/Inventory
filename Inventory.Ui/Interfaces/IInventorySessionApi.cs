using Inventory.Dto.InventorySessions.Requests;
using Inventory.Dto.InventorySessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IInventorySessionApi
    {
        [Post("/api/v1/inventorySessions")]
        Task<InventorySessionResult> Create(
            [Body] CreateInventorySessionRequest request);

        [Get("/api/v1/inventorySessions/{id}")]
        Task<InventorySessionResult> GetById(Guid id);

        [Get("/api/v1/inventorySessions")]
        Task<List<InventorySessionResult>> GetAll();

        [Put("/api/v1/inventorySessions/{id}")]
        Task<InventorySessionResult> Update(
            Guid id,
            [Body] UpdateInventorySessionRequest request);

        [Delete("/api/v1/inventorySessions/{id}")]
        Task Delete(Guid id);

        [Get("/api/v1/inventorySessions/search")]
        Task<PagedResult<InventorySessionResult>> Search(
            [Query] InventorySessionQuery query);
    }
}
