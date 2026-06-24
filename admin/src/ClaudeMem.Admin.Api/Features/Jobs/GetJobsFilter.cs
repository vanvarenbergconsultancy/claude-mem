using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Jobs;

internal sealed record GetJobsFilter(
    string? Status = null,
    string? ProjectId = null,
    string? Cursor = null,
    int? PageSize = null) : ICursorFilter;
