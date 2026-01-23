using Inventory.Dto.CustomerTransactions.Requests;
using Inventory.Dto.CustomerTransactions.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Stock.Requests;
using Inventory.Dto.Stock.Results;
using Inventory.Dto.StockMouvements.Requests;
using Inventory.Dto.StockMouvements.Results;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IProductApi
    {
        [Post("/api/v1/products")]
        Task<ProductResult> Create([Body] CreateProductRequest request);

        [Get("/api/v1/products")]
        Task<List<ProductResult>> GetAll();

        [Get("/api/v1/products/{id}")]
        Task<ProductResult> GetById(Guid id);

        [Put("/api/v1/products/{id}")]
        Task<ProductResult> Update(
            Guid id,
            [Body] UpdateProductRequest request);

        [Delete("/api/v1/products/{id}")]
        Task Delete(Guid id);

        [Get("/api/v1/products/search")]
        Task<PagedResult<ProductResult>> Search(
            [Query] ProductQuery query);
    }
}
