using System.Collections.Generic;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

/// <summary>The raw result of a cursor-paged database query before it is mapped to a response.</summary>
internal sealed record CursorPageResult<T>(
    IReadOnlyList<T> Items,
    string? SelfCursor,
    string? NextCursor);
