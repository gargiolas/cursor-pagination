using CursorPagination.Application.Abstractions;
using CursorPagination.Application.DTOs;
using CursorPagination.Domain.Users;
using CursorPagination.Persistence;
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
    GetPagedUserCursorQueryHandler(ApplicationContext applicationContext)
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
    /// Retrieves a paginated list of users, based on the provided cursor-based pagination parameters.
    /// </summary>
    /// <param name="request">The request containing pagination parameters, such as cursor and direction (next or previous).</param>
    /// <param name="sizeLimit">The maximum number of users to retrieve at one time.</param>
    /// <param name="cancellationToken">An optional token to cancel the asynchronous operation.</param>
    /// <returns>A result collection containing the retrieved users and pagination metadata (cursor and has-more flag).</returns>
    private async Task<ResultCollection<UserDto>> GetPagedUsersAsync(
        GetPagedUserCursorQuery request, 
        int sizeLimit,
        CancellationToken cancellationToken = default)
    {
        var index = GetCursorIndex(request);

        var userQuery = (request.Cursor, request.IsNext) switch
        {
            (null, false) => throw new ArgumentException("Filter is required"),
            (_, _) => GetUserCursor(sizeLimit, index, request.IsNext)
        };

        var entries = await userQuery
            .Select(EntryQueries.ToUserDto())
            .ToListAsync(cancellationToken: cancellationToken);

        var hasNextPage = entries.Count > sizeLimit;

        string? nextCursor = null;
        if (hasNextPage)
        {
            var lastEntry = entries[^1]; //The Last entry is the one we want to return
            nextCursor = Cursor<UserDto>.Encode(lastEntry.Id, lastEntry, (index + sizeLimit));
          
            //Remove last entity used to generate the next page
            entries.RemoveAt(entries.Count - 1);
        }

        var paginationResult = new ResultCollection<UserDto>()
        {
            Items = entries,
            Cursor = nextCursor,
            HasPrevious = index > 0,
            HasMore = hasNextPage
        };

        return paginationResult;
    }

    /// Retrieves an IQueryable collection of users based on the specified limit, cursor index, and the direction of pagination.
    /// <param name="limit">The maximum number of users to retrieve.</param>
    /// <param name="minIndex">The minimum cursor index used for pagination.</param>
    /// <param name="isNext">Indicates if the pagination is moving forward (true) or backward (false).</param>
    /// <returns>An IQueryable of User entities to be processed further in the query.</returns>
    private IQueryable<User> GetUserCursor(int limit, int minIndex, bool isNext = true)
    {
        return applicationContext.Set<User>().FromSqlInterpolated($"""
                                                                   SELECT 
                                                                       st."Id",
                                                                       st."Surname",
                                                                       st."Name",
                                                                       st."Email",
                                                                       st."Address"
                                                                   FROM (
                                                                       SELECT 
                                                                           ROW_NUMBER() OVER (ORDER BY c."Surname" DESC, c."Name" ASC) AS "RowNum",
                                                                           c."Id",
                                                                           c."Surname",
                                                                           c."Name",
                                                                           c."Email",
                                                                           c."Address"
                                                                       FROM cursor_pagination.users c
                                                                   ) AS st
                                                                   WHERE "RowNum" > {minIndex - (!isNext ? limit : 0)}
                                                                   LIMIT {limit + 1}
                                                                   """);
    }

    /// <summary>
    /// Determines the minimum cursor index based on the given query request.
    /// </summary>
    /// <param name="request">The request containing cursor details and navigation direction.</param>
    /// <returns>The minimum cursor index to begin paginated querying.</returns>
    /// <exception cref="ArgumentException">Thrown when the cursor is invalid or insufficient for backward navigation.</exception>
    private static int GetCursorIndex(GetPagedUserCursorQuery request)
    {
        if (request.Cursor is null) return 0;

        var cursor = Cursor<UserDto>.Decode(request.Cursor);

        if (cursor is null) return 0;

        var lastIndex = cursor?.LastIndexOfEntity ??
                        throw new ArgumentException("Cursor is required");

        if (lastIndex == 0 && !request.IsNext) return 0;

        return lastIndex;
    }
}