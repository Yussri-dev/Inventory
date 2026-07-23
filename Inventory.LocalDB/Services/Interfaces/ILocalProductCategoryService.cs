using Inventory.Dto.ProductCategory.Requests;
using Inventory.Dto.ProductCategory.Results;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalProductCategoryService
    {
        Task<ProductCategoryResult> CreateAsync(
            CreateProductCategoryRequest request, 
            CancellationToken ct= default);

        Task<ProductCategoryResult> UpdateAsync(
            Guid id, 
            UpdateProductCategoryRequest request,
            CancellationToken ct = default);

        Task DeleteAsync(Guid id, CancellationToken ct = default);

        Task<List<ProductCategoryResult>> GetAllAsync(CancellationToken ct = default);
    }
}
