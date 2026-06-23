using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Features.Common;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.ApiKeys.Create;

internal sealed record CreateApiKeyCommand(string TeamId, string ProjectId, string ActorId) : ICommand<Result<NewApiKey>>, ITeamProjectKey;
