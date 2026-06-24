using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Teams;

internal interface ITeamAccess
{
    Task<bool> TeamExists(string teamId, CancellationToken cancellationToken);
    Task<CursorPageResult<Team>> GetTeams(GetTeamsFilter filter, CancellationToken cancellationToken);
    Task<Team?> GetTeamById(string teamId, CancellationToken cancellationToken);
    Task<Team> CreateTeam(string name, CancellationToken cancellationToken);
}
