// Provides support for expression trees
// Used to build dynamic LINQ queries
namespace Inventory.Infrastructure.Repositories
{
    public interface IReadRepository<T> where T : class
    {
        IQueryable<T> Query();
    }
}
