using System.Linq.Expressions;

namespace CursorPagination.Domain.Abstractions;

internal interface IRepository<TEntity> where TEntity : class
{
    // Get single entity by ID
    Task<TEntity?> GetByIdAsync(object id);

    // Get all entities
    Task<IEnumerable<TEntity>> GetAllAsync();

    // Find entity/entities with a filter expression
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

    // Add a new entity
    Task AddAsync(TEntity entity);

    // Add multiple entities
    Task AddRangeAsync(IEnumerable<TEntity> entities);

    // Remove an entity
    void Remove(TEntity entity);

    // Remove multiple entities
    void RemoveRange(IEnumerable<TEntity> entities);

    // Update an entity
    void Update(TEntity entity);
}
