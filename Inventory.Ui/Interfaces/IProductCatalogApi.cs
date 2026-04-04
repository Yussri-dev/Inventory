using Inventory.Dto.Pages.Results;
using Inventory.Dto.ProductCatalogs.Requests;
using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Purchases.Requests;
using Inventory.Dto.Purchases.Results;
using Inventory.Dto.Queries;
using Inventory.Dto.Suppliers.Requests;
using Inventory.Dto.Suppliers.Results;
using Refit;
using System.Net.Http;

namespace Inventory.Ui.Interfaces
{
    public interface IProductCatalogApi
    {
        [Post("/api/v1/productcatalogs")]
        Task<ProductCatalogResult> Create(
            [Body] CreateProductCatalogRequest request);

        [Get("/api/v1/productcatalogs")]
        Task<List<ProductCatalogResult>> GetAll();

        [Get("/api/v1/productcatalogs/{id}")]
        Task<ProductCatalogResult> GetById(Guid id);

        [Put("/api/v1/productcatalogs/{id}")]
        Task<ProductCatalogResult> Update(
            Guid id,
            [Body] UpdateProductCatalogRequest request);

        [Delete("/api/v1/productcatalogs/{id}")]
        Task<HttpResponseMessage> Delete(Guid id);

        [Get("/api/v1/productcatalogs/search")]
        Task<PagedResult<ProductCatalogResult>> Search(
            [Query] ProductCatalogQuery query);
    }

}
