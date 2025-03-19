using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace CursorPagination.Application.Abstractions;

/// <summary>
/// Represents a cursor utilized in pagination for tracking position and metadata associated with a specific entity.
/// </summary>
/// <typeparam name="TEntity">The type of the entity related to the cursor.</typeparam>
/// <param name="LastId">The unique identifier of the most recent entity within the cursor context.</param>
/// <param name="Entity">The entity object corresponding to the current position in the paginated set.</param>
/// <param name="Position">The numerical position of the current entity relative to the dataset.</param>
public sealed record Cursor<TEntity>(Guid LastId, TEntity Entity, int Position)
{
    /// <summary>
    /// Generates an encoded cursor string and provides the entity index for pagination purposes.
    /// </summary>
    /// <typeparam name="T">The type of the entity associated with the cursor.</typeparam>
    /// <param name="lastId">The unique identifier of the last entity in the current result set.</param>
    /// <param name="entity">The entity object associated with the cursor.</param>
    /// <param name="position">The position or index of the entity in the result set.</param>
    /// <returns>
    /// A tuple containing the encoded cursor string and the entity index.
    /// </returns>
    public static (string EncodedCursor, int EntityIndex) GenerateEncodedCursor<T>(
        Guid lastId,
        T entity,
        int position)
    {
        var cursorObject = new Cursor<T>(lastId, entity, position);
        string serializedCursor = JsonSerializer.Serialize(cursorObject);
        string encodedCursor = Base64UrlTextEncoder.Encode(Encoding.UTF8.GetBytes(serializedCursor));
        return (encodedCursor, position);
    }

    /// <summary>
    /// Decodes a Base64URL encoded cursor string into a <see cref="Cursor{TEntity}"/> object.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity represented by the cursor.</typeparam>
    /// <param name="encodedCursor">The Base64URL encoded cursor string to decode. Can be null or empty.</param>
    /// <returns>
    /// A <see cref="Cursor{TEntity}"/> object containing the decoded information if valid;
    /// otherwise, null if the input is invalid or cannot be parsed.
    /// </returns>
    private static Cursor<TEntity>? DecodeCursor(string? encodedCursor)
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
    /// Calculates the index of the cursor based on the provided encoded cursor, page size, and direction (next or previous page).
    /// </summary>
    /// <param name="encodedCursor">
    /// The encoded cursor that represents the current position and entity. Can be null.
    /// </param>
    /// <param name="pageSize">
    /// The size of the page to be retrieved. Must be a positive integer.
    /// </param>
    /// <param name="isNextPage">
    /// A boolean indicating the direction for pagination.
    /// If true, the index for the next page is calculated; if false, the index for the previous page is calculated.
    /// </param>
    /// <returns>
    /// The calculated cursor index. If the encoded cursor is null or invalid, returns 0.
    /// </returns>
    public static int CalculateCursorIndex(
        string? encodedCursor,
        int pageSize,
        bool isNextPage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var decodedCursor = DecodeCursor(encodedCursor);
        if (decodedCursor is null) return 0;

        var lastIndex = decodedCursor.Position;

        return isNextPage ? lastIndex + pageSize : Math.Max(0, lastIndex - pageSize);
    }

    /// <summary>
    /// Decodes a Base64-encoded string into its JSON string representation.
    /// </summary>
    /// <param name="encodedCursor">The Base64-encoded string input to be decoded.</param>
    /// <returns>The decoded JSON string representation of the input.</returns>
    private static string DecodeBase64ToJson(string encodedCursor)
    {
        byte[] decodedBytes = Base64UrlTextEncoder.Decode(encodedCursor);
        return Encoding.UTF8.GetString(decodedBytes);
    }
}