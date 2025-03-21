using System.Text.Json.Serialization;
using CursorPagination.Domain.Users;

namespace CursorPagination.Application.Features.Users.Queries.GetPagedUserCursorQuery;

internal sealed class UserResponse : User
{
    [JsonIgnore]
    public long RowIndex { get; set; }
}