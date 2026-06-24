using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Projects.Get;

internal sealed record GetProjectsQuery(string TeamId, CursorRequest Cursor) : IQuery<Result<CursorPageResult<Project>>>;
