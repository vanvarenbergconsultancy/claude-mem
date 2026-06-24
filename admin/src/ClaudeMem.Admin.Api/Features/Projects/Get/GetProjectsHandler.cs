using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Features.Teams;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;
using ClaudeMem.Admin.Api.Features.Projects.Shared;

namespace ClaudeMem.Admin.Api.Features.Projects.Get;

internal sealed class GetProjectsHandler : IQueryHandler<GetProjectsQuery, Result<CursorPageResult<Project>>>
{
    private readonly IProjectAccess _projectAccess;
    private readonly ITeamAccess _teamAccess;

    public GetProjectsHandler(IProjectAccess projectAccess, ITeamAccess teamAccess)
    {
        _projectAccess = projectAccess;
        _teamAccess = teamAccess;
    }

    public async ValueTask<Result<CursorPageResult<Project>>> Handle(GetProjectsQuery query, CancellationToken cancellationToken)
    {
        var teamExists = await _teamAccess.TeamExists(query.TeamId, cancellationToken);
        if (!teamExists)
        {
            return Result.Fail<CursorPageResult<Project>>(ResultErrorCodes.TeamNotFound.AsError());
        }

        var filter = new GetProjectsFilter(
            TeamId: query.TeamId,
            Cursor: query.Cursor.Cursor,
            PageSize: query.Cursor.PageSize);

        var projectsPage = await _projectAccess.GetProjects(filter, cancellationToken);

        return Result.Ok(projectsPage);
    }
}
