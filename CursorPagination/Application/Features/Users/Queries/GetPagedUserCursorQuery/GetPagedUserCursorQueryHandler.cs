using CursorPagination.Application.Abstractions;
using CursorPagination.Application.Abstractions.Services;
using CursorPagination.Application.DTOs;
using CursorPagination.Domain.Users;
using CursorPagination.Persistence;
using CursorPagination.Persistence.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CursorPagination.Application.Features.Users.Queries.GetPagedUserCursorQuery;

/// <summary>
/// Handles the query for retrieving a paginated collection of users based on a cursor for navigation.
/// </summary>
/// <remarks>
/// This query handler supports cursor-based pagination using forward and backward navigation.
/// It retrieves a limited subset of user data, including pagination metadata such as
/// next cursor and availability of more data.
/// </remarks>
/// <param name="applicationContext">The database context used to retrieve user data.</param>
/// <seealso cref="GetPagedUserCursorQuery"/>
/// <seealso cref="ResultCollection{UserDto}"/>
internal sealed class
    GetPagedUserCursorQueryHandler(ApplicationContext applicationContext, ICursorService cursorService)
    : IRequestHandler<GetPagedUserCursorQuery, ResultCollection<UserDto>>
{
    /// <summary>
    /// Handles the processing of the GetPagedUserCursorQuery to retrieve a paginated collection of UserDto objects
    /// based on cursor-based pagination.
    /// </summary>
    /// <param name="request">The query containing the cursor and direction for fetching paginated users.</param>
    /// <param name="cancellationToken">A token that may be used to cancel the operation.</param>
    /// <returns>A result collection containing a list of UserDto objects, the next cursor if applicable, and a flag indicating if there are more records.</returns>
    public async Task<ResultCollection<UserDto>> Handle(GetPagedUserCursorQuery request,
        CancellationToken cancellationToken)
    {
        const int limit = 10;
        return await GetPagedUsersAsync(request, limit, cancellationToken);
    }

    /// <summary>
    /// Asynchronously retrieves a paginated list of users based on cursor-based pagination parameters.
    /// </summary>
    /// <param name="request">The query containing the cursor and direction (next or previous) for pagination.</param>
    /// <param name="sizeLimit">The maximum number of users to retrieve in the current request.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A result collection containing the list of UserDto objects, the generated cursor for the next or previous page, and metadata indicating if there are more records available.</returns>
    private async Task<ResultCollection<UserDto>> GetPagedUsersAsync(
        GetPagedUserCursorQuery request,
        int sizeLimit,
        CancellationToken cancellationToken = default)
    {
        var calculateCursorIndex =
            cursorService.CalculateCursorIndex(request.Cursor, sizeLimit, request.IsNext, request.UserFilter);

        if (string.IsNullOrWhiteSpace(request.Cursor) && !request.IsNext)
            throw new ArgumentException("Filter is required");

        var userQuery = applicationContext.ToRanked<User>("cursor_pagination.users",
            [
                "Id",
                "Surname",
                "Name",
                "Email",
                "Address"
            ],
            [
                ("Surname", false),
                ("Name", true)
            ], startIndex: calculateCursorIndex, recordForPage: sizeLimit);

        var entries = await userQuery
            .Select(EntryQueries.ToUserDto())
            .ToListAsync(cancellationToken: cancellationToken);

        var lastEntry = entries[^1]; //The Last entry is the one we want to return
        var cursor =
            cursorService.GenerateEncodedCursor(lastEntry.Id, request.UserFilter, calculateCursorIndex);

        var hasMore = entries.Count > sizeLimit;

        //Remove the last entity used to generate the next page
        entries.RemoveAt(entries.Count - 1);

        var paginationResult = new ResultCollection<UserDto>()
        {
            Items = entries,
            Cursor = cursor.EncodedCursor,
            HasPrevious = cursor.EntityIndex - sizeLimit > 0,
            HasMore = hasMore
        };

        return paginationResult;
    }
}