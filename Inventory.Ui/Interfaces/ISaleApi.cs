using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Sales.Requests;
using Inventory.Dto.Sales.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface ISaleApi
    {
        [Post("/api/v1/sales")]
        Task<SaleResult> Create(
            [Body] CreateSaleRequest request);

        [Post("/api/v1/sales/complete")]
        Task<CreateCompleteSaleResult> CreateComplete(
            [Body] CreateCompleteSaleRequest request);


        [Post("/api/v1/sales/pending")]
        Task<SaleResult> CreatePending(    [Body] CreatePendingSaleRequest request);


        [Get("/api/v1/sales")]
        Task<List<SaleResult>> GetAll();

        [Get("/api/v1/sales/{id}")]
        Task<SaleResult> GetById(Guid id);

        [Put("/api/v1/sales/{id}")]
        Task<SaleResult> Update(
            Guid id,
            [Body] UpdateSaleRequest request);

        [Delete("/api/v1/sales/{id}")]
        Task Delete(Guid id);

        [Get("/api/v1/sales/search")]
        Task<PagedResult<SaleResult>> Search(
            [Query] SaleQuery query);

        [Get("/api/v1/sales/by-customer/{customerId}")]
        Task<PagedResult<SaleResult>> GetByCustomer(Guid customerId, [Query] CustomerSaleQuery query);

        [Get("/api/v1/sales/pending")]
        Task<List<SaleResult>> GetPending();

        [Get("/api/v1/sales/pending/{id}")]
        Task<SaleResult> GetPendingById(Guid id);

        [Put("/api/v1/sales/pending/{id}")]
        Task<SaleResult> UpdatePending(Guid id, [Body] CreatePendingSaleRequest request);

        [Get("/api/v1/sales/history")]
        Task<PagedResult<SaleResult>> GetHistory([Query] SaleHistoryQuery query);
        //[Get("/api/v1/sales/{id}/ticket")]
        //Task<HttpResponseMessage> GetTicket(Guid id);

    }
}
