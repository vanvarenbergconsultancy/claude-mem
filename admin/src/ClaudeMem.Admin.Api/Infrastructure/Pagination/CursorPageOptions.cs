using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

/// <summary>Resolved pagination options derived from a caller-supplied <see cref="ICursorFilter"/>.</summary>
internal sealed class CursorPageOptions
{
    public const int DefaultPageSize = 20;
    public const int MinPageSize = 1;
    public const int MaxPageSize = 100;

    private readonly ICursorCodec _codec;

    private CursorPageOptions(int pageSize, CursorPayload? decodedCursor, ICursorCodec codec)
    {
        PageSize = pageSize;
        FetchCount = pageSize + 1;
        DecodedCursor = decodedCursor;
        _codec = codec;
    }

    /// <summary>Parses and validates the filter, decoding the cursor token if present.</summary>
    public static async Task<CursorPageOptions> Create(ICursorFilter filter, ICursorCodec codec, CancellationToken cancellationToken = default)
    {
        var pageSize = CalculatePageSize(filter.PageSize);
        var decodedCursor = await codec.Detokenize(filter.Cursor, cancellationToken);

        return new CursorPageOptions(pageSize, decodedCursor, codec);
    }

    /// <summary>The resolved number of items per page.</summary>
    public int PageSize { get; }

    /// <summary>PageSize + 1 — used to detect whether a next page exists.</summary>
    public int FetchCount { get; }

    /// <summary>The decoded cursor identifying the last record of the previous page, or <c>null</c> for the first page.</summary>
    public CursorPayload? DecodedCursor { get; }

    /// <summary>Trims the sentinel row, then tokenises and returns the next-page cursor. Returns <c>null</c> when there is no next page.</summary>
    public async Task<string?> TrimAndGetNextCursor<T>(List<T> rows, Func<T, CursorPayload> getPayload, CancellationToken cancellationToken = default)
    {
        if (rows.Count < FetchCount)
        {
            return null;
        }

        rows.RemoveAt(rows.Count - 1);

        var cursor = await _codec.Tokenize(getPayload(rows[^1]), cancellationToken);

        return cursor;
    }

    private static int CalculatePageSize(int? filterPageSize)
    {
        var pageSize = filterPageSize ?? DefaultPageSize;

        // forces a number to stay within a range. If it's already inside, it passes through unchanged. If it's outside, it's pinned to the nearest boundary.
        return Math.Clamp(pageSize, MinPageSize, MaxPageSize);
    }
}
