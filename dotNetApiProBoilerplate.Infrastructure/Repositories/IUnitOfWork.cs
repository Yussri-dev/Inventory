using Microsoft.EntityFrameworkCore.Storage;

namespace Inventory.Infrastructure.Repositories
{
    // Unit of Work interface
    // Coordinates the saving of changes across multiple repositories
    // Ensures all operations are committed as a single transaction
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default);

        IDbContextTransaction? CurrentTransaction { get; }

        Task CommitTransactionAsync(
            CancellationToken cancellationToken = default);

        Task RollbackTransactionAsync(
            CancellationToken cancellationToken = default);
    }
}
