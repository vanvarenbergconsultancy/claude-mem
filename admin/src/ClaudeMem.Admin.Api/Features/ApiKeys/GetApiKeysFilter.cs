using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.ApiKeys;

internal sealed record GetApiKeysFilter(
    string? ProjectId = null,
    string? Cursor = null,
    int? PageSize = null) : ICursorFilter;
