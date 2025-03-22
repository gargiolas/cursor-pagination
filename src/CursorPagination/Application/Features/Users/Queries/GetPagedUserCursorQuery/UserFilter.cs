using CursorPagination.Application.Abstractions.Services;

namespace CursorPagination.Application.Features.Users.Queries.GetPagedUserCursorQuery;

public sealed class UserFilter : ICursorFilter
{
    public string Name { get; set; }
}