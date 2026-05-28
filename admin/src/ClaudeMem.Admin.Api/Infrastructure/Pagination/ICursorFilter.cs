namespace ClaudeMem.Admin.Api.Infrastructure.Pagination;

internal interface ICursorFilter
{
    string? Cursor { get; }
    int? PageSize { get; }
}
