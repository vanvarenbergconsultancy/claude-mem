namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

/// <summary>
/// Represents the cursor-based pagination parameters supplied by a caller.
/// Implemented by every query/filter record that supports paged results.
/// </summary>
internal interface ICursorFilter
{
    /// <summary>
    /// The opaque pagination cursor returned by the previous page.
    /// <c>null</c> indicates the first page.
    /// </summary>
    string? Cursor { get; }

    /// <summary>
    /// The number of items to return per page.
    /// Clamped to <see cref="CursorPageOptions.MinPageSize"/>–<see cref="CursorPageOptions.MaxPageSize"/>.
    /// <c>null</c> uses <see cref="CursorPageOptions.DefaultPageSize"/>.
    /// </summary>
    int? PageSize { get; }
}
