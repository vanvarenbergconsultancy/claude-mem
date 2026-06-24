using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Teams;

/// <summary>Database access contract for team operations.</summary>
internal interface ITeamAccess
{
    /// <summary>Returns <c>true</c> if a team with the given ID exists.</summary>
    Task<bool> TeamExists(string teamId, CancellationToken cancellationToken);

    /// <summary>Returns a cursor-paged list of teams matching the filter.</summary>
    Task<CursorPageResult<Team>> GetTeams(GetTeamsFilter filter, CancellationToken cancellationToken);

    /// <summary>Returns the team with the given ID, or <c>null</c> if not found.</summary>
    Task<Team?> GetTeamById(string teamId, CancellationToken cancellationToken);

    /// <summary>Inserts a new team and returns the created record.</summary>
    Task<Team> CreateTeam(string name, CancellationToken cancellationToken);
}
