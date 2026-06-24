using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Features.Teams;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;
using ClaudeMem.Admin.Api.Features.Projects.Shared;

namespace ClaudeMem.Admin.Api.Features.Projects.Create;

internal sealed class CreateProjectHandler : ICommandHandler<CreateProjectCommand, Result<Project>>
{
    private readonly IProjectAccess _projectAccess;
    private readonly ITeamAccess _teamAccess;

    public CreateProjectHandler(IProjectAccess projectAccess, ITeamAccess teamAccess)
    {
        _projectAccess = projectAccess;
        _teamAccess = teamAccess;
    }

    public async ValueTask<Result<Project>> Handle(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        var teamExists = await _teamAccess.TeamExists(command.TeamId, cancellationToken);
        if (!teamExists)
        {
            return Result.Fail<Project>(ResultErrorCodes.TeamNotFound.AsError());
        }

        var createdProject = await _projectAccess.CreateProject(command.TeamId, command.Name, cancellationToken);

        return Result.Ok(createdProject);
    }
}
