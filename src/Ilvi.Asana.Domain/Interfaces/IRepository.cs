using System.Linq.Expressions;

namespace Ilvi.Asana.Domain.Interfaces;

/// <summary>
/// Generic repository interface
/// </summary>
public interface IRepository<T> where T : class
{
    // Query
    Task<T?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> ExistsAsync(long id, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    IQueryable<T> Query();

    // Commands
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);

    // Bulk operations
    Task BulkInsertAsync(IList<T> entities, CancellationToken ct = default);
    Task BulkUpdateAsync(IList<T> entities, CancellationToken ct = default);
    Task BulkInsertOrUpdateAsync(IList<T> entities, CancellationToken ct = default);
    Task BulkDeleteAsync(IList<T> entities, CancellationToken ct = default);

    // Get IDs
    Task<List<long>> GetAllIdsAsync(CancellationToken ct = default);
}
