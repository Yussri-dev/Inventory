using Inventory.Dto.Damages.Requests;
using Inventory.Dto.Damages.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IDamageApi
    {
        [Post("/api/v1.0/damages")]
        Task<DamageResult> Create([Body] CreateDamageRequest request);

        [Post("/api/v1.0/damages/validate")]
        Task ValidateAll();

        [Get("/api/v1.0/damages/{id}")]
        Task<DamageResult> GetById(Guid id);

        [Get("/api/v1.0/damages")]
        Task<List<DamageResult>> GetAll();

        [Get("/api/v1.0/damages/search")]
        Task<PagedResult<DamageResult>> Search([Query] DamageQuery query);

        [Put("/api/v1.0/damages/{id}")]
        Task<DamageResult> Update(Guid id, [Body] UpdateDamageRequest request);

        [Delete("/api/v1.0/damages/{id}")]
        Task Delete(Guid id);
    }
}
