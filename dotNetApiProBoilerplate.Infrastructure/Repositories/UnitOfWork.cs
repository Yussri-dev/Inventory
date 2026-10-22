using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Inventory.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly InventoryDbContext _context;

        public UnitOfWork(
            InventoryDbContext context)
        {
            _context = context;
        }

        public IDbContextTransaction? CurrentTransaction => _context.Database.CurrentTransaction;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(
                cancellationToken);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_context.Database.CurrentTransaction != null)
            {
                throw new InvalidOperationException("A database transaction is already active.");
            }

            return await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = _context.Database.CurrentTransaction;

            if (transaction == null)
            {
                throw new InvalidOperationException(
                    "There is no active transaction to commit.");
            }

            await transaction.CommitAsync(cancellationToken);
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = _context.Database.CurrentTransaction;

            if (transaction == null)
            {
                return;
            }

            await transaction.RollbackAsync(cancellationToken);
        }
    }
}