using System.Text;
using System.Text.Json;
using CursorPagination.Application.Abstractions;
using CursorPagination.Application.Abstractions.Services;
using CursorPagination.Domain.Abstractions;
using Microsoft.AspNetCore.Authentication;

namespace CursorPagination.Infrastructure.Services;

/// <summary>
/// Provides functionality for implementing cursor-based pagination, offering methods to generate encoded cursors,
/// decode cursors, and calculate the index of an entity within a paginated dataset.
/// </summary>
internal sealed class CursorService : ICursorService
{
    /// <summary>
    /// Generates an encoded cursor string along with the entity's index within a paginated dataset.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity associated with the cursor.</typeparam>
    /// <param name="lastId">The unique identifier of the most recent entity in the dataset.</param>
    /// <param name="entity">The entity object to be embedded in the cursor.</param>
    /// <param name="position">The numerical position of the entity within the dataset.</param>
    /// <returns>A tuple containing the encoded cursor as a string and the entity's index in the dataset.</returns>
    public (string EncodedCursor, int EntityIndex) GenerateEncodedCursor<TEntity>(Guid lastId, TEntity entity,
        int position) where TEntity : ICursorFilter
    {
        var cursorObject = new Cursor<TEntity>(lastId, entity, position);
        var serializedCursor = JsonSerializer.Serialize(cursorObject);
        var encodedCursor = Base64UrlTextEncoder.Encode(Encoding.UTF8.GetBytes(serializedCursor));
        return (encodedCursor, position);
    }

    /// <summary>
    /// Calculates the cursor index for pagination based on the provided cursor, page size, navigation direction, and associated entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity associated with the cursor, implementing the <see cref="ICursorFilter"/> interface.</typeparam>
    /// <param name="encodedCursor">The base64 encoded cursor string representing the last cursor state. Can be null.</param>
    /// <param name="pageSize">The number of items per page. Must be a positive integer.</param>
    /// <param name="isNextPage">Indicates whether to calculate the index for the next page (true) or the previous page (false).</param>
    /// <param name="entity">The entity object that implement <see cref="ICursorFilter"/>.</param>
    /// <returns>An integer representing the calculated cursor index for the requested page.</returns>
    public int CalculateCursorIndex<TEntity>(string? encodedCursor, int pageSize, bool isNextPage, TEntity entity)
        where TEntity : ICursorFilter
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        ArgumentNullException.ThrowIfNull(entity);

        var decodedCursor = DecodeCursor<TEntity>(encodedCursor);
        if (decodedCursor is null) return 0;

        var comparer = new EntityFilterEqualityComparer<TEntity>();
        var isMatch = comparer.Equals(decodedCursor.Entity, entity);

        if (!isMatch) return 0;

        var lastIndex = decodedCursor.Position;

        return (isNextPage, lastIndex) switch
        {
            (true, _) => lastIndex + pageSize,
            (false, _) => (lastIndex != pageSize) ? lastIndex - pageSize : 0
        };
    }

    /// <summary>
    /// Decodes an encoded cursor string into a strongly typed <see cref="Cursor{TEntity}"/> object.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity associated with the cursor.</typeparam>
    /// <param name="encodedCursor">The base64-encoded string representing the serialized cursor data.</param>
    /// <returns>
    /// A <see cref="Cursor{TEntity}"/> object if decoding is successful; otherwise, null if the input is invalid or deserialization fails.
    /// </returns>
    private static Cursor<TEntity>? DecodeCursor<TEntity>(string? encodedCursor) where TEntity : ICursorFilter
    {
        if (string.IsNullOrWhiteSpace(encodedCursor)) return null;

        try
        {
            string decodedJson = DecodeBase64ToJson(encodedCursor);
            return JsonSerializer.Deserialize<Cursor<TEntity>>(decodedJson);
        }
        catch (JsonException) // Gestisce errori JSON specifici
        {
            return null;
        }
    }

    /// <summary>
    /// Decodes a Base64 encoded string into its JSON representation.
    /// </summary>
    /// <param name="encodedCursor">The Base64 encoded string to decode.</param>
    /// <returns>A JSON string resulting from the decoded Base64 input.</returns>
    private static string DecodeBase64ToJson(string encodedCursor)
    {
        byte[] decodedBytes = Base64UrlTextEncoder.Decode(encodedCursor);
        return Encoding.UTF8.GetString(decodedBytes);
    }
}