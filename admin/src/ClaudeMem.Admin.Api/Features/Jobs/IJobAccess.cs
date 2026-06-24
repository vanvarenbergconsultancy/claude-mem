using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Jobs;

internal interface IJobAccess
{
    Task<CursorPageResult<Job>> GetJobs(GetJobsFilter filter, CancellationToken cancellationToken);
    Task<string?> GetJobStatus(string jobId, CancellationToken cancellationToken);
    Task ResetJobToQueued(string jobId, CancellationToken cancellationToken);
}
