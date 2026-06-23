using System;
using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Jobs.Retry;

internal sealed class RetryJobHandler : ICommandHandler<RetryJobCommand, Result<Unit>>
{
    private readonly IJobAccess _jobAccess;

    public RetryJobHandler(IJobAccess jobAccess)
    {
        _jobAccess = jobAccess;
    }

    public async ValueTask<Result<Unit>> Handle(RetryJobCommand command, CancellationToken cancellationToken)
    {
        var jobStatus = await _jobAccess.GetJobStatus(command.JobId, cancellationToken);

        if (jobStatus is null)
        {
            return Result.Fail<Unit>(ResultErrorCodes.JobNotFound.AsError());
        }

        var jobIsInFailedState = string.Equals(jobStatus, "failed", StringComparison.OrdinalIgnoreCase);
        if (!jobIsInFailedState)
        {
            return Result.Fail<Unit>(ResultErrorCodes.JobNotInFailedState.AsError());
        }

        await _jobAccess.ResetJobToQueued(command.JobId, cancellationToken);

        return Result.Ok(Unit.Value);
    }
}
