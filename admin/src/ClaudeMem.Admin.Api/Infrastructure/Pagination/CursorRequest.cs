namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

internal sealed record CursorRequest(string? Cursor, int? PageSize = null) : ICursorFilter;
