using Microsoft.EntityFrameworkCore;

namespace CursorPagination.Persistence;

public class ApplicationContext : BaseContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base("cursor_pagination", options)
    {
    }
}