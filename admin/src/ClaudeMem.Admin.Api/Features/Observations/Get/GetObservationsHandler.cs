using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Features.Projects.Shared;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Observations.Get;

internal sealed class GetObservationsHandler : IQueryHandler<GetObservationsQuery, Result<CursorPageResult<Observation>>>
{
    private readonly IObservationAccess _observationAccess;
    private readonly IProjectAccess _projectAccess;

    public GetObservationsHandler(IObservationAccess observationAccess, IProjectAccess projectAccess)
    {
        _observationAccess = observationAccess;
        _projectAccess = projectAccess;
    }

    public async ValueTask<Result<CursorPageResult<Observation>>> Handle(GetObservationsQuery query, CancellationToken cancellationToken)
    {
        var teamAndProjectExistence = await _projectAccess.CheckProjectTeamExistence(query.TeamId, query.ProjectId, cancellationToken);
        var teamDoesNotHaveAccessToProjectError = teamAndProjectExistence.ValidateTeamAndProjectIdMisMatch();
        if (teamDoesNotHaveAccessToProjectError.HasValue)
        {
            return Result.Fail<CursorPageResult<Observation>>(teamDoesNotHaveAccessToProjectError.Value.AsError());
        }

        var filter = new GetObservationsFilter(
            TeamId: query.TeamId,
            ProjectId: query.ProjectId,
            SearchText: query.SearchText,
            Cursor: query.Cursor.Cursor,
            PageSize: query.Cursor.PageSize);

        var observationsPage = await _observationAccess.GetObservations(filter, cancellationToken);

        return Result.Ok(observationsPage);
    }
}
