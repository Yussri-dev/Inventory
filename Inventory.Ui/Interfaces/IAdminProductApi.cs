using Inventory.Dto.Pages.Results;
using Inventory.Dto.Products.Results;
using Inventory.Dto.Queries;
using Refit;

namespace Inventory.Ui.Interfaces
{
    public interface IAdminProductApi
    {
        [Get("/api/admin/products/search")]
        Task<PagedResult<ProductResult>> Search(
            [Query] ProductQuery query,
            CancellationToken cancellationToken = default);
    }
}
