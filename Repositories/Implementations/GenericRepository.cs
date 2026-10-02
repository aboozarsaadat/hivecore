using System.Linq.Expressions;
using HiveCore.Data;
using HiveCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HiveCore.Repositories.Implementations;

/// <summary>
/// Generic EF Core repository implementation.
/// </summary>
public class GenericRepository<TEntity> : IGenericRepository<TEntity>
    where TEntity : class
{
    protected readonly HiveCoreDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(HiveCoreDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
    }

    // ============================================================
    // Query
    // ============================================================
    public virtual async Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object?[] { id }, cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;

        foreach (var include in includes)
            query = query.Include(include);

        // Find by primary key — we assume the entity has "Id" property
        return await query.FirstOrDefaultAsync(
            e => EF.Property<Guid>(e, "Id") == id,
            cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        return predicate == null
            ? await _dbSet.CountAsync(cancellationToken)
            : await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public virtual IQueryable<TEntity> Query() => _dbSet.AsQueryable();

    public virtual IQueryable<TEntity> Query(Expression<Func<TEntity, bool>> predicate)
        => _dbSet.Where(predicate);

    // ============================================================
    // Command
    // ============================================================
    public virtual async Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var entry = await _dbSet.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    public virtual async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Update(TEntity entity) => _dbSet.Update(entity);

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
        => _dbSet.UpdateRange(entities);

    public virtual void Remove(TEntity entity) => _dbSet.Remove(entity);

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
        => _dbSet.RemoveRange(entities);

    // ============================================================
    // Pagination
    // ============================================================
    public virtual async Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;

        IQueryable<TEntity> query = _dbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        var total = await query.CountAsync(cancellationToken);

        if (orderBy != null)
            query = orderBy(query);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    // ============================================================
    // Unit of Work
    // ============================================================
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public virtual async Task<IDisposable> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public virtual async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var tx = _context.Database.CurrentTransaction;
        if (tx != null)
            await tx.CommitAsync(cancellationToken);
    }

    public virtual async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var tx = _context.Database.CurrentTransaction;
        if (tx != null)
            await tx.RollbackAsync(cancellationToken);
    }
}