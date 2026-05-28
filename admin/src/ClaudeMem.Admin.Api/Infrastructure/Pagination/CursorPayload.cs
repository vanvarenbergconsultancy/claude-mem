using System;
using System.Text.Json;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

internal sealed record CursorPayload(string Id, DateTimeOffset CreatedAt)
{
    public static string Encode(CursorPayload payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);

        return Convert.ToBase64String(bytes);
    }

    public static CursorPayload? Decode(string? cursor)
    {
        if (cursor is null)
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(cursor);

            return JsonSerializer.Deserialize<CursorPayload>(bytes);
        }
        catch
        {
            return null;
        }
    }
}
