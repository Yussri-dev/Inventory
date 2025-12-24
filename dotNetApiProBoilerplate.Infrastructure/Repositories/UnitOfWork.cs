// Import the application's Entity Framework Core DbContext
// Used to persist changes made through repositories
using Inventory.Infrastructure.Data;

namespace Inventory.Infrastructure.Repositories
{
    // Unit of Work implementation
    // Responsible for committing all pending changes as a single operation
    public class UnitOfWork : IUnitOfWork
    {
        // DbContext instance shared across repositories
        // Ensures consistency within a single request/transaction scope
        private readonly InventoryDbContext _context;

        // Constructor injection of DbContext
        // DbContext lifetime is managed by the DI container (scoped)
        public UnitOfWork(InventoryDbContext context)
        {
            _context = context;
        }

        // Persists all tracked changes to the database asynchronously
        // This includes inserts, updates, and deletes performed by repositories
        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
