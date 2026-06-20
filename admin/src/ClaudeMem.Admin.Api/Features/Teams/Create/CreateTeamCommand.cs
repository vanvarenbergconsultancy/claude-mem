using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Teams.Create;

internal sealed record CreateTeamCommand(string Name) : ICommand<Result<Team>>;
