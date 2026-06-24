using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Projects.Create;

internal sealed record CreateProjectCommand(string TeamId, string Name) : ICommand<Result<Project>>;
