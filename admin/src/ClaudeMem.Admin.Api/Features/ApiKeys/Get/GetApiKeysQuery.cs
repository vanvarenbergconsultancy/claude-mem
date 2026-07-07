using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Features.Common;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.ApiKeys.Get;

internal sealed record GetApiKeysQuery(string TeamId, string ProjectId, CursorRequest Cursor) : IQuery<Result<CursorPageResult<ApiKey>>>, ITeamProjectKey;
