using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

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

    public static async Task<CursorPageOptions> Create(ICursorFilter filter, ICursorCodec codec, CancellationToken cancellationToken = default)
    {
        var pageSize = CalculatePageSize(filter.PageSize);
        var decodedCursor = await codec.Detokenize(filter.Cursor, cancellationToken);

        return new CursorPageOptions(pageSize, decodedCursor, codec);
    }

    public int PageSize { get; }
    public int FetchCount { get; }
    public CursorPayload? DecodedCursor { get; }

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
