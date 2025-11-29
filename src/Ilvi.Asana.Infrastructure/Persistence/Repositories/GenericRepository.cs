using System.Linq.Expressions;
using EFCore.BulkExtensions;
using Ilvi.Asana.Domain.Entities;
using Ilvi.Asana.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ilvi.Asana.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic repository implementation
/// </summary>
public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly AsanaDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AsanaDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    #region Query Methods

    public async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, ct);
    }

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<List<T>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.ToListAsync(ct);
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(ct);
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken ct = default)
    {
        var entity = await _dbSet.FindAsync(new object[] { id }, ct);
        if (entity != null)
        {
            _context.Entry(entity).State = EntityState.Detached;
        }
        return entity != null;
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(predicate, ct);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        return await _dbSet.CountAsync(ct);
    }

    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _dbSet.CountAsync(predicate, ct);
    }

    public IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }

    #endregion

    #region Command Methods

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        await _dbSet.AddRangeAsync(entities, ct);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    #endregion

    #region Bulk Operations

    public async Task BulkInsertAsync(IList<T> entities, CancellationToken ct = default)
    {
        if (!entities.Any()) return;
        await _context.BulkInsertAsync(entities, cancellationToken: ct);
    }

    public async Task BulkUpdateAsync(IList<T> entities, CancellationToken ct = default)
    {
        if (!entities.Any()) return;
        await _context.BulkUpdateAsync(entities, cancellationToken: ct);
    }

    public async Task BulkInsertOrUpdateAsync(IList<T> entities, CancellationToken ct = default)
    {
        if (!entities.Any()) return;
        
        var config = new BulkConfig
        {
            SetOutputIdentity = false,
            PreserveInsertOrder = false,
            BatchSize = 1000,
            BulkCopyTimeout = 180,
            PropertiesToExcludeOnUpdate = new List<string> { "CreatedAt" }
        };

        await _context.BulkInsertOrUpdateAsync(entities, config, cancellationToken: ct);
    }

    public async Task BulkDeleteAsync(IList<T> entities, CancellationToken ct = default)
    {
        if (!entities.Any()) return;
        await _context.BulkDeleteAsync(entities, cancellationToken: ct);
    }

    #endregion

    #region ID Operations

    public async Task<List<long>> GetAllIdsAsync(CancellationToken ct = default)
    {
        // BaseEntity'den türeyen entity'ler için
        if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
        {
            return await _dbSet
                .Cast<BaseEntity>()
                .Select(e => e.Id)
                .ToListAsync(ct);
        }

        throw new NotSupportedException($"{typeof(T).Name} does not inherit from BaseEntity");
    }

    #endregion
}
