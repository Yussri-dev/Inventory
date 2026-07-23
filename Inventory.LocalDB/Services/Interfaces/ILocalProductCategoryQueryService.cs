using Inventory.Dto.ProductCategory.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalProductCategoryQueryService
    {
        Task<List<ProductCategoryResult>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task<ProductCategoryResult?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<bool> HasLocalDataAsync(
            CancellationToken cancellationToken = default);
    }
}
