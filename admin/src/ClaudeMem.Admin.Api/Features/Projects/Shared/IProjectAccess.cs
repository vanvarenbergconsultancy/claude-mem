using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Projects.Shared;

/// <summary>Database access contract for project operations.</summary>
internal interface IProjectAccess
{
    /// <summary>Checks whether the team exists and the project belongs to that team.</summary>
    Task<ProjectTeamExistence> CheckProjectTeamExistence(string teamId, string projectId, CancellationToken cancellationToken);

    /// <summary>Returns a cursor-paged list of projects matching the filter.</summary>
    Task<CursorPageResult<Project>> GetProjects(GetProjectsFilter filter, CancellationToken cancellationToken);

    /// <summary>Returns the project scoped to the given team, or <c>null</c> if not found.</summary>
    Task<Project?> GetProjectById(string teamId, string projectId, CancellationToken cancellationToken);

    /// <summary>Inserts a new project under the given team and returns the created record.</summary>
    Task<Project> CreateProject(string teamId, string name, CancellationToken cancellationToken);
}
