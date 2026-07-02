using Inventory.LocalDB.Models;

namespace Inventory.LocalDB.Services.Interfaces
{
    public interface ILocalSaleService
    {
        Task<LocalSale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<List<LocalSale>> GetTodaySalesAsync(CancellationToken cancellationToken = default);

        Task<LocalSale> CreateAsync(LocalSale sale, CancellationToken cancellationToken = default);
    }
}
