using CursorPagination.Domain.Abstractions;

namespace CursorPagination.Persistence.Repositories;

internal sealed class UnitOfWork(ApplicationContext context) : IUnitOfWork
{
    private readonly Dictionary<Type, object> _repositories = new();

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        if (_repositories.ContainsKey(typeof(TEntity))) return (IRepository<TEntity>)_repositories[typeof(TEntity)];
        
        var repository = new Repository<TEntity>(context);
        _repositories.Add(typeof(TEntity), repository);

        return (IRepository<TEntity>)_repositories[typeof(TEntity)];
    }

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync();
    }

    public void Dispose()
    {
        context.Dispose();
    }
}
