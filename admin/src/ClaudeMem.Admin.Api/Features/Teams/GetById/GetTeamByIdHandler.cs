using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Results;
using Mediator;

namespace ClaudeMem.Admin.Api.Features.Teams.GetById;

internal sealed class GetTeamByIdHandler : IQueryHandler<GetTeamByIdQuery, Result<Team>>
{
    private readonly ITeamAccess _teamAccess;

    public GetTeamByIdHandler(ITeamAccess teamAccess)
    {
        _teamAccess = teamAccess;
    }

    public async ValueTask<Result<Team>> Handle(GetTeamByIdQuery query, CancellationToken cancellationToken)
    {
        var teamDetail = await _teamAccess.GetTeamById(query.TeamId, cancellationToken);
        if (teamDetail is null)
        {
            return Result.Fail<Team>(ResultErrorCodes.TeamNotFound.AsError());
        }

        return Result.Ok(teamDetail);
    }
}
