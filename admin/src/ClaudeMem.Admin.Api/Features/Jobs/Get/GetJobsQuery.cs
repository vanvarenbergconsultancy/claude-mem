using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Jobs.Get;

internal sealed record GetJobsQuery(CursorRequest Cursor, string? Status, string? ProjectId) : IQuery<Result<CursorPageResult<Job>>>;
