using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Teams.GetById;

internal sealed record GetTeamByIdQuery(string TeamId) : IQuery<Result<Team>>;
