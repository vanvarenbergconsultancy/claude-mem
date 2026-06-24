using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Features.Projects.Shared;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Projects.GetById;

internal sealed class GetProjectByIdHandler : IQueryHandler<GetProjectByIdQuery, Result<Project>>
{
    private readonly IProjectAccess _projectAccess;

    public GetProjectByIdHandler(IProjectAccess projectAccess)
    {
        _projectAccess = projectAccess;
    }

    public async ValueTask<Result<Project>> Handle(GetProjectByIdQuery query, CancellationToken cancellationToken)
    {
        var teamAndProjectExistence = await _projectAccess.CheckProjectTeamExistence(query.TeamId, query.ProjectId, cancellationToken);
        var teamDoesNotHaveAccessToProjectError = teamAndProjectExistence.ValidateTeamAndProjectIdMisMatch();
        if (teamDoesNotHaveAccessToProjectError.HasValue)
        {
            return Result.Fail<Project>(teamDoesNotHaveAccessToProjectError.Value.AsError());
        }

        var projectDetail = await _projectAccess.GetProjectById(query.TeamId, query.ProjectId, cancellationToken);

        return Result.Ok(projectDetail!);
    }
}
