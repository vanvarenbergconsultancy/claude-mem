using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Features.Common;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Projects.GetById;

internal sealed record GetProjectByIdQuery(string TeamId, string ProjectId) : IQuery<Result<Project>>, ITeamProjectKey;
