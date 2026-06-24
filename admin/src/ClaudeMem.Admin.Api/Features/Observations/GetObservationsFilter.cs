using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Observations;

internal sealed record GetObservationsFilter(
    string? TeamId = null,
    string? ProjectId = null,
    string? SearchText = null,
    string? Cursor = null,
    int? PageSize = null) : ICursorFilter;
