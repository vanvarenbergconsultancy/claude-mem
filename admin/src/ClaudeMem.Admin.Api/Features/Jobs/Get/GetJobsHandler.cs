using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Jobs.Get;

internal sealed class GetJobsHandler : IQueryHandler<GetJobsQuery, Result<CursorPageResult<Job>>>
{
    private readonly IJobAccess _jobAccess;

    public GetJobsHandler(IJobAccess jobAccess)
    {
        _jobAccess = jobAccess;
    }

    public async ValueTask<Result<CursorPageResult<Job>>> Handle(GetJobsQuery query, CancellationToken cancellationToken)
    {
        var filter = new GetJobsFilter(
            Status: query.Status,
            ProjectId: query.ProjectId,
            Cursor: query.Cursor.Cursor,
            PageSize: query.Cursor.PageSize);

        var jobsPage = await _jobAccess.GetJobs(filter, cancellationToken);

        return Result.Ok(jobsPage);
    }
}
