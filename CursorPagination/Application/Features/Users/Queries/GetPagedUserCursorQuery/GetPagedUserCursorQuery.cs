using CursorPagination.Application.Abstractions;
using CursorPagination.Application.DTOs;
using MediatR;

namespace CursorPagination.Application.Features.Users.Queries.GetPagedUserCursorQuery;

internal sealed record GetPagedUserCursorQuery(string? Cursor, bool IsNext) : IRequest<ResultCollection<UserDto>>;