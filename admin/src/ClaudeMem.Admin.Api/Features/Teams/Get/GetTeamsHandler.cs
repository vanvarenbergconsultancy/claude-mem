using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Teams.Get;

internal sealed class GetTeamsHandler : IQueryHandler<GetTeamsQuery, Result<CursorPageResult<Team>>>
{
    private readonly ITeamAccess _teamAccess;

    public GetTeamsHandler(ITeamAccess teamAccess)
    {
        _teamAccess = teamAccess;
    }

    public async ValueTask<Result<CursorPageResult<Team>>> Handle(GetTeamsQuery query, CancellationToken cancellationToken)
    {
        var filter = new GetTeamsFilter(query.Cursor.Cursor, query.Cursor.PageSize);

        var teamsPage = await _teamAccess.GetTeams(filter, cancellationToken);

        return Result.Ok(teamsPage);
    }
}
