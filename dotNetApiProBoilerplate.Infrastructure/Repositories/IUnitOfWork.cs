namespace Inventory.Infrastructure.Repositories
{
    // Unit of Work interface
    // Coordinates the saving of changes across multiple repositories
    // Ensures all operations are committed as a single transaction
    public interface IUnitOfWork
    {
        // Persists all pending changes to the database asynchronously
        // Returns the number of affected records
        Task<int> SaveChangesAsync();
    }
}
