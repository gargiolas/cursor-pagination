namespace CursorPagination.Application.Abstractions.Services;

/// <summary>
/// Defines methods and operations to support cursor-based pagination, including generating cursors and determining entity positions.
/// </summary>
public interface ICursorService
{
    /// <summary>
    /// Generates an encoded cursor string along with the entity's index within a paginated dataset.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity associated with the cursor.</typeparam>
    /// <param name="lastId">The unique identifier of the most recent entity in the dataset.</param>
    /// <param name="entity">The entity object to be embedded in the cursor.</param>
    /// <param name="position">The numerical position of the entity within the dataset.</param>
    /// <returns>A tuple containing the encoded cursor as a string and the entity's index in the dataset.</returns>
    public (string EncodedCursor, int EntityIndex) GenerateEncodedCursor<TEntity>(
        Guid lastId,
        TEntity entity,
        int position)
        where TEntity : ICursorFilter;

    /// <summary>
    /// Calculates the cursor index for paginated data based on the provided encoded cursor,
    /// page size, navigation direction, and entity details.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity associated with the cursor operation.</typeparam>
    /// <param name="encodedCursor">
    /// A base64-encoded string representing the current cursor state; can be null if starting from the beginning.
    /// </param>
    /// <param name="pageSize">
    /// The number of items per page. Must be a positive integer.
    /// </param>
    /// <param name="isNextPage">
    /// A boolean value indicating whether to calculate the index for the next page (true)
    /// or the previous page (false).
    /// </param>
    /// <param name="entity">The entity related to the cursor calculation.</param>
    /// <returns>An integer representing the calculated cursor index based on the provided inputs.</returns>
    public int CalculateCursorIndex<TEntity>(
        string? encodedCursor,
        int pageSize,
        bool isNextPage,
        TEntity entity)
        where TEntity : ICursorFilter;
}