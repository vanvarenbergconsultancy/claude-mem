using System.Collections.Generic;

namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

internal sealed record CursorPageResult<T>(
    IReadOnlyList<T> Items,
    string? SelfCursor,
    string? NextCursor);
