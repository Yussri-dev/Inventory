using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.Pages.Results;
using Inventory.Dto.Queries;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalProductCatalogQueryService
    {
        Task<PagedResult<ProductCatalogResult>> QueryAsync(
            ProductCatalogQuery query,
            CancellationToken cancellationToken = default);

        Task<ProductCatalogResult?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<List<ProductCatalogResult>> SearchUnitCatalogsAsync(
            string? search,
            Guid? excludeCatalogId = null,
            int take = 30,
            CancellationToken cancellationToken = default);
        Task<bool> HasLocalDataAsync(
        CancellationToken cancellationToken = default);
    }
}