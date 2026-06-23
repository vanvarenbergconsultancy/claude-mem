using ClaudeMem.Admin.Api.Features.Common;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.ApiKeys.Delete;

internal sealed record DeleteApiKeyCommand(string TeamId, string ProjectId, string KeyId) : ICommand<Result<Unit>>, ITeamProjectKey;
