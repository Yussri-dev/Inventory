using Inventory.Dto.ProductCatalogs.Results;
using Inventory.Dto.ProductCategory.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventory.Ui.Services.Sync
{
    public interface ILocalProductCatalogSyncService
    {
        Task FullSyncAsync(
            CancellationToken cancellationToken = default);

        Task UpsertAsync(
            ProductCatalogResult catalog,
            CancellationToken cancellationToken = default);

        Task MarkDeletedAsync(
            Guid catalogId,
            CancellationToken cancellationToken = default);

        Task<bool> HasLocalDataAsync(
            CancellationToken cancellationToken = default);
    }
}
