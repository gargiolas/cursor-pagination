using System.Linq.Expressions;
using CursorPagination.Application.DTOs;
using CursorPagination.Domain.Users;

namespace CursorPagination.Application.Features.Users.Queries.GetPagedUserCursorQuery;

internal static class EntryQueries
{
    internal static Expression<Func<User, UserDto>> ToUserDto()
    {
        return entry => new UserDto()
        {
            Id = entry.Id,
            Name = entry.Name,
            Surname = entry.Surname,
            Email = entry.Email,
        };
    }
}