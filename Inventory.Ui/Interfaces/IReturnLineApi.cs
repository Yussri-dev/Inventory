using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.ReturnLines.Requests;
using Inventory.Dto.ReturnLines.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IReturnLineApi
    {
        [Post("/api/v1/returnLines")]
        Task<ReturnLineResult> Create([Body] CreateReturnLineRequest request);

        [Get("/api/v1/returnLines/{id}")]
        Task<ReturnLineResult> GetById(Guid id);

        [Get("/api/v1/returnLines")]
        Task<List<ReturnLineResult>> GetAll();

        [Put("/api/v1/returnLines/{id}")]
        Task<ReturnLineResult> Update(Guid id, [Body] UpdateReturnLineRequest request);

        [Delete("/api/v1/returnLines/{id}")]
        Task Delete(Guid id);

        [Get("/api/v1/returnLines/search")]
        Task<PagedResult<ReturnLineResult>> Search([Query] ReturnLineQuery query);
    }
}
