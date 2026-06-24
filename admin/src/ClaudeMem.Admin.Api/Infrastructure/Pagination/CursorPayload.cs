using System;
using System.Text.Json;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

/// <summary>The structured data embedded inside a pagination cursor token.</summary>
internal sealed record CursorPayload(string Id, DateTimeOffset CreatedAt)
{
    /// <summary>Serialises and base-64 encodes the payload into an opaque cursor string.</summary>
    public static string Encode(CursorPayload payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);

        return Convert.ToBase64String(bytes);
    }

    /// <summary>Decodes a base-64 cursor string back to a payload, returning <c>null</c> on invalid input.</summary>
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
