using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Teams.Get;

internal sealed record GetTeamsQuery(CursorRequest Cursor) : IQuery<Result<CursorPageResult<Team>>>;
