using Inventory.Dto.Pages.Results;
using Inventory.Dto.ProductCategory.Requests;
using Inventory.Dto.ProductCategory.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IProductCategoryApi
    {
        [Post("/api/v1/productCategory")]
        Task<ProductCategoryResult> Create(
            [Body] CreateProductCategoryRequest request);

        [Get("/api/v1/productCategory")]
        Task<List<ProductCategoryResult>> GetAll();

        [Get("/api/v1/productCategory/{id}")]
        Task<ProductCategoryResult> GetById(Guid id);

        [Put("/api/v1/productCategory/{id}")]
        Task<ProductCategoryResult> Update(
            Guid id,
            [Body] UpdateProductCategoryRequest request);

        [Delete("/api/v1/productCategory/{id}")]
        Task<HttpResponseMessage> Delete(Guid id);

        [Get("/api/v1/productCategory/search")]
        Task<PagedResult<ProductCategoryResult>> Search(
            [Query] ProductCategoryQuery query);
    }

}
