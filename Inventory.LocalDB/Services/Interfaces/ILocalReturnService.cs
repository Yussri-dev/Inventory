using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Results;

namespace Inventory.LocalDB.Services.Interfaces;

public interface ILocalReturnService
{
    Task<IReadOnlyList<LocalReturnableSaleResult>> SearchSalesAsync(
        string search,
        int maximumResults = 20,
        CancellationToken cancellationToken = default);

    Task<LocalReturnableSaleResult?> GetReturnableSaleAsync(
        Guid localSaleId,
        CancellationToken cancellationToken = default);

    Task<LocalReturn?> GetByIdAsync(
        Guid localReturnId,
        CancellationToken cancellationToken = default);

    Task<LocalReturn> CreateAsync(
        LocalReturn localReturn,
        CancellationToken cancellationToken = default);
}
