using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Customers.Requests;
using Inventory.Dto.Customers.Results;
using Refit;
using Inventory.Dto.LoyaltyCards.Results;
using Inventory.Dto.LoyaltyCards.Requests;

namespace Inventory.Ui.Interfaces
{
    public interface ICustomerApi
    {
        [Post("/api/v1/customers")]
        Task<CustomerResult> Create(CreateCustomerRequest request);

        [Get("/api/v1/customers")]
        Task<List<CustomerResult>> GetAll();

        [Put("/api/v1/customers/{id}")]
        Task<CustomerResult> Update(Guid id, UpdateCustomerRequest request);

        [Delete("/api/v1/customers/{id}")]
        Task Delete(Guid id);

        [Get("/api/v1/customers/search")]
        Task<PagedResult<CustomerResult>> Search([Query] CustomerQuery query);
    }
}
