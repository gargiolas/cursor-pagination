namespace CursorPagination.Application.Abstractions;

public class ResultCollection<TResult> where TResult : class
{
    public List<TResult> Items { get; set; } = new();
    public string? Cursor { get; set; } = null;
    public bool HasPrevious { get; set; }
    public bool HasMore { get; set; }
}