// Import domain entities
// Not strictly required for the generic repository itself,
// but commonly included for consistency across infrastructure files
using Inventory.Domain.Entities;

// Import the application's EF Core DbContext
// Provides access to database and entity tracking
using Inventory.Infrastructure.Data;

// Entity Framework Core APIs (DbSet, async LINQ extensions)
using Microsoft.EntityFrameworkCore;

// Expression trees used for dynamic LINQ filtering
using System.Linq.Expressions;

namespace Inventory.Infrastructure.Repositories
{
    // Generic repository implementation
    // Provides reusable data access logic for any entity type
    public class Repository<T> : IRepository<T> where T : class
    {
        // Protected DbContext instance
        // Accessible to derived repositories if specialization is needed
        protected readonly InventoryDbContext _context;

        // DbSet representing the table for entity T
        // Used to perform CRUD and query operations
        protected readonly DbSet<T> _dbSet;

        // Repository constructor
        // DbContext is injected via dependency injection
        public Repository(InventoryDbContext context)
        {
            _context = context;

            // Resolve the DbSet for the given entity type T
            _dbSet = context.Set<T>();
        }

        // Adds a new entity to the DbSet asynchronously
        // The entity is tracked but not persisted until SaveChangesAsync is called
        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        // Retrieves an entity by its primary key
        // Uses EF Core's FindAsync for optimized lookups
        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        // Retrieves all entities of type T
        // Returns the full table content
        public async Task<List<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        // Marks an entity as modified
        // EF Core will generate an UPDATE statement on SaveChangesAsync
        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        // Marks an entity for deletion
        // EF Core will generate a DELETE statement on SaveChangesAsync
        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        // Checks if any entity matches the given predicate
        // Efficient for existence checks without loading entities
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        // Retrieves all entities matching a filter predicate
        // Useful for custom queries and filtering logic
        public async Task<List<T>> GetAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        // Retrieves the first entity matching a predicate
        // Returns null if no entity matches
        public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        // Counts entities optionally filtered by a predicate
        // If predicate is null, counts all entities
        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();

            return await _dbSet.CountAsync(predicate);
        }
    }
}
