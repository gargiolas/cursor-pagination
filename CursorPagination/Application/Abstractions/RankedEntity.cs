namespace CursorPagination.Application.Abstractions;

public sealed class RankedEntity<TEntity>
{
    public int Rank { get; set; }
    public TEntity Entity { get; set; }
}