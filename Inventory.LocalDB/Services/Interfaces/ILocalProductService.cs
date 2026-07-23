using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Requests;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using Inventory.LocalDB.Models;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalProductService
    {
        Task<ProductResult> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

        Task<ProductResult> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        Task<PagedResult<ProductResult>> QueryAsync(ProductQuery query, CancellationToken cancellationToken = default);

        Task<List<ProductResult>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<LocalProduct?> GetByBarcodeAsync(string barcode);

        Task<LocalProductScanResult?> ResolveBarcodeAsync(string barcode);

        Task UpsertAsync(LocalProduct product);

        Task<List<LocalProduct>> SearchAsync(string search, int take = 50);
    }
}
