using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace CursorPagination.Application.Abstractions;

/// <summary>
/// Represents a cursor used for pagination operations, including metadata and the current entity reference.
/// </summary>
/// <typeparam name="TEntity">The type of the entity attached to the cursor.</typeparam>
/// <param name="LastId">The unique identifier of the last entity in the current cursor.</param>
/// <param name="Entity">The entity representation of the current cursor position.</param>
/// <param name="Position">The numeric index representing the position of the entity within a paginated dataset.</param>
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

    /// Decodes a Base64URL encoded cursor string into a <see cref="Cursor{TEntity}"/> object.
    /// <param name="encodedCursor">
    /// The Base64URL encoded cursor string to decode. Can be null or empty.
    /// </param>
    /// <returns>
    /// A <see cref="Cursor{TEntity}"/> object containing the decoded information if the input is valid;
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
    /// Calculates the next or previous cursor position and retrieves the related entity based on the provided encoded cursor.
    /// </summary>
    /// <param name="encodedCursor">
    /// The encoded cursor that contains information about the last position and entity. Can be null.
    /// </param>
    /// <param name="pageSize">
    /// The size of the page to be retrieved. Must be greater than zero.
    /// </param>
    /// <param name="isNextPage">
    /// Indicates whether the next page or the previous page is to be retrieved.
    /// When true, calculates the cursor for the next page; when false, calculates for the previous page.
    /// </param>
    /// <returns>
    /// A tuple containing the index of the entity and the entity itself.
    /// If the encoded cursor is null or invalid, returns a default value (0, null).
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
    /// Decodes a Base64-encoded string into a JSON string representation.
    /// </summary>
    /// <param name="encodedCursor">The Base64-encoded string to decode.</param>
    /// <returns>The decoded JSON string representation.</returns>
    private static string DecodeBase64ToJson(string encodedCursor)
    {
        byte[] decodedBytes = Base64UrlTextEncoder.Decode(encodedCursor);
        return Encoding.UTF8.GetString(decodedBytes);
    }
}