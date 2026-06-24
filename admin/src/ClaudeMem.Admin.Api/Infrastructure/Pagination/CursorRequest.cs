namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

/// <summary>A simple <see cref="ICursorFilter"/> for use cases that only need cursor and page-size parameters.</summary>
internal sealed record CursorRequest(string? Cursor, int? PageSize = null) : ICursorFilter;
