using System;
using System.Collections.Generic;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

internal sealed class CursorPageOptions
{
    public const int DefaultPageSize = 20;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;

    private readonly ICursorEncoder _encoder;

    public CursorPageOptions(ICursorFilter filter, ICursorEncoder encoder)
    {
        _encoder = encoder;
        PageSize = CalculatePageSize(filter.PageSize);
        FetchCount = PageSize + 1;
        DecodedCursor = encoder.Decode(filter.Cursor);
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

        return _encoder.Encode(getPayload(rows[^1]));
    }

    private static int CalculatePageSize(int? filterPageSize)
    {
        var pageSize = filterPageSize ?? DefaultPageSize;
        var pageSizeBetweenMinMaxOrNearestBound = Math.Clamp(pageSize, MinPageSize, MaxPageSize);
        
        return pageSizeBetweenMinMaxOrNearestBound;
    }
}
