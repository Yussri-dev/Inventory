// Provides support for expression trees
// Used to build dynamic LINQ queries
using System.Linq.Expressions;

namespace Inventory.Infrastructure.Repositories
{
    // Generic repository interface
    // Defines a common contract for data access operations
    // T represents an entity type mapped by EF Core
    public interface IRepository<T> where T : class
    {
        // -------------------------
        // Basic CRUD operations
        // -------------------------

        // Adds a new entity to the data store asynchronously
        // The entity is tracked by the DbContext
        Task AddAsync(T entity);

        // Retrieves an entity by its unique identifier
        // Returns null if no entity is found
        Task<T?> GetByIdAsync(Guid id);

        // Retrieves all entities of type T
        // Should be used carefully to avoid large result sets
        Task<List<T>> GetAllAsync();

        // Marks an existing entity as modified
        // Changes are persisted when UnitOfWork.SaveChangesAsync is called
        void Update(T entity);

        // Marks an entity for deletion
        // Actual deletion occurs on SaveChangesAsync
        void Delete(T entity);

        // -------------------------
        // Query and utility methods
        // -------------------------

        // Checks whether any entity matches the given predicate
        // Useful for existence checks (e.g. uniqueness validation)
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        // Retrieves all entities matching the given predicate
        // Allows flexible filtering using LINQ expressions
        Task<List<T>> GetAsync(Expression<Func<T, bool>> predicate);

        // Retrieves a single entity matching the predicate
        // Returns null if no match is found
        Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate);

        // Counts entities matching the predicate
        // If predicate is null, counts all entities
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        Task<T?> GetLastAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderByDesc
        );

        Task<List<T>> GetAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes
);

    }
}
