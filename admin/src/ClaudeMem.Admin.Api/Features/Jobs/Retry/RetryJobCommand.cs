using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Jobs.Retry;

internal sealed record RetryJobCommand(string JobId) : ICommand<Result<Unit>>;
