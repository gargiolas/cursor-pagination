using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace CursorPagination.Application.Abstractions;

public sealed record Cursor<TEntity>(Guid LastId, TEntity Entity, int LastIndexOfEntity)
{
    public static string Encode<TEntityToEncode>(Guid lastId, TEntityToEncode entity, int lastIndexOfEntity)
    {
        var cursor = new Cursor<TEntityToEncode>(lastId, entity, lastIndexOfEntity);
        var json = JsonSerializer.Serialize(cursor);
        return Base64UrlTextEncoder.Encode(Encoding.UTF8.GetBytes(json));
    }

    public static Cursor<TEntity>? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            string json = Encoding.UTF8.GetString(Base64UrlTextEncoder.Decode(cursor));
            return JsonSerializer.Deserialize<Cursor<TEntity>>(json);
        }
        catch
        {
            return null;
        }
    }
}