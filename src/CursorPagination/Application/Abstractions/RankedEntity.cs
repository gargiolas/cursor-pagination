namespace CursorPagination.Application.Abstractions;

public sealed class RankedEntity<TEntity> where TEntity : class
{
    public long Rank { get; set; }
    public TEntity Entity { get; set; }
}