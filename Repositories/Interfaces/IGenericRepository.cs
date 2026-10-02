using System.Linq.Expressions;

namespace HiveCore.Repositories.Interfaces;

/// <summary>
/// Generic repository interface providing common CRUD operations
/// for any entity with a Guid primary key.
/// </summary>
public interface IGenericRepository<TEntity> where TEntity : class
{
    // ============================================================
    // Query
    // ============================================================
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    IQueryable<TEntity> Query();

    IQueryable<TEntity> Query(Expression<Func<TEntity, bool>> predicate);

    // ============================================================
    // Command
    // ============================================================
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void UpdateRange(IEnumerable<TEntity> entities);

    void Remove(TEntity entity);

    void RemoveRange(IEnumerable<TEntity> entities);

    // ============================================================
    // Pagination
    // ============================================================
    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    // ============================================================
    // Unit of Work
    // ============================================================
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}