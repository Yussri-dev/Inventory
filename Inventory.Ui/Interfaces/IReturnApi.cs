using Inventory.Dto.Analytics.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Returns.Requests;
using Inventory.Dto.Returns.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IReturnApi
    {
        [Post("/api/v1/returns")]
        Task<ReturnResult> Create([Body] CreateReturnRequest request);

        [Post("/api/v1/returns/complete")]
        Task<ReturnResult> CreateComplete([Body] CreateCompleteReturnRequest request);

        [Get("/api/v1/returns/{id}")]
        Task<ReturnResult> GetById(Guid id);

        [Get("/api/v1/returns")]
        Task<List<ReturnResult>> GetAll();

        [Put("/api/v1/returns/{id}")]
        Task<ReturnResult> Update(Guid id, [Body] UpdateReturnRequest request);

        [Delete("/api/v1/returns/{id}")]
        Task Delete(Guid id);

        [Get("/api/v1/returns/search")]
        Task<PagedResult<ReturnResult>> Search([Query] ReturnQuery query);
    }
}
