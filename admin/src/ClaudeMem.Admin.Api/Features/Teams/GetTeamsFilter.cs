using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Teams;

internal sealed record GetTeamsFilter(
    string? Cursor = null,
    int? PageSize = null) : ICursorFilter;
