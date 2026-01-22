using Inventory.Dto.CashSessions.Requests;
using Inventory.Dto.CashSessions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface ICashSessionApi
    {
        [Get("/api/v1/cashsessions/active")]
        Task<CashSessionResult?> GetActive();

        [Post("/api/v1/cashsessions")]
        Task<CashSessionResult> Create([Body] CreateCashSessionRequest request);

        [Post("/api/v1/cashsessions/{id}/close")]
        Task<CashSessionResult> Close(Guid id, [Body] CloseCashSessionRequest request);

        [Get("/api/v1/cashsessions")]
        Task<List<CashSessionResult>> GetAll();

        [Get("/api/v1/cashsessions/{id}")]
        Task<CashSessionResult> GetById(Guid id);

        [Delete("/api/v1/cashsessions/{id}")]
        Task Delete(Guid id);

        [Get("/api/v1/cashsessions/search")]
        Task<PagedResult<CashSessionResult>> Search([Query] CashSessionQuery query);
    }
}
