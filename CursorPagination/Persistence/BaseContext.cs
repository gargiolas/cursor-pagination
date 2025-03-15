using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CursorPagination.Persistence;

public abstract class BaseContext : DbContext, IDatabase
{
    private readonly string _schemaName;
    protected BaseContext(string schemaName, DbContextOptions options)
        : base(options)
    {
        _schemaName = schemaName;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}

internal interface IDatabase
{
    DatabaseFacade Database { get; }
}