using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Features.Common;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Observations.Get;

internal sealed record GetObservationsQuery(string TeamId, string ProjectId, CursorRequest Cursor, string? SearchText) : IQuery<Result<CursorPageResult<Observation>>>, ITeamProjectKey;
