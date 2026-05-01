// Provides support for expression trees
// Used to build dynamic LINQ queries
using System.Linq.Expressions;

namespace Inventory.Infrastructure.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task AddAsync(T entity);

        Task<T?> GetByIdAsync(Guid id);

        Task<List<T>> GetAllAsync();

        void Update(T entity);

        void Delete(T entity);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        Task<List<T>> GetAsync(Expression<Func<T, bool>> predicate);

        Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate);

        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        Task<T?> GetLastAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderByDesc
        );

        Task<List<T>> GetAsync(
            Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includes);

        Task AddRangeAsync(IEnumerable<T> entities);

        void DeleteRange(IEnumerable<T> entities);

        IQueryable<T> Query();

    }
}
