using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Projects.Shared;

internal sealed record GetProjectsFilter(
    string? TeamId = null,
    string? Cursor = null,
    int? PageSize = null) : ICursorFilter;