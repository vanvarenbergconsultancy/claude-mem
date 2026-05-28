using System;
using System.Collections.Generic;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

internal sealed class CursorPageOptions
{
    public const int DefaultPageSize = 20;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;

    public CursorPageOptions(ICursorFilter filter)
    {
        PageSize = Math.Clamp(filter.PageSize ?? DefaultPageSize, MinPageSize, MaxPageSize);
        FetchCount = PageSize + 1;
        DecodedCursor = CursorPayload.Decode(filter.Cursor);
    }

    public int PageSize { get; }
    public int FetchCount { get; }
    public CursorPayload? DecodedCursor { get; }

    public string? TrimAndGetNextCursor<T>(List<T> rows, Func<T, CursorPayload> getPayload)
    {
        if (rows.Count < FetchCount)
        {
            return null;
        }

        rows.RemoveAt(rows.Count - 1);
        return CursorPayload.Encode(getPayload(rows[^1]));
    }
}
