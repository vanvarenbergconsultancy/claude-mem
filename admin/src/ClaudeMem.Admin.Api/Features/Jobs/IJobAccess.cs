using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Jobs;

/// <summary>Database access contract for job operations.</summary>
internal interface IJobAccess
{
    /// <summary>Returns a cursor-paged list of jobs matching the filter.</summary>
    Task<CursorPageResult<Job>> GetJobs(GetJobsFilter filter, CancellationToken cancellationToken);

    /// <summary>Returns the current status string of the given job, or <c>null</c> if not found.</summary>
    Task<string?> GetJobStatus(string jobId, CancellationToken cancellationToken);

    /// <summary>Resets a failed job back to the <c>queued</c> state so it can be retried.</summary>
    Task ResetJobToQueued(string jobId, CancellationToken cancellationToken);
}
