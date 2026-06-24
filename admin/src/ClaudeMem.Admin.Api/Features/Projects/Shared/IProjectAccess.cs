using System.Threading;
using System.Threading.Tasks;
using ClaudeMem.Admin.Api.Contracts;
using ClaudeMem.Admin.Api.Infrastructure.Pagination;

namespace ClaudeMem.Admin.Api.Features.Projects.Shared;

internal interface IProjectAccess
{
    Task<ProjectTeamExistence> CheckProjectTeamExistence(string teamId, string projectId, CancellationToken cancellationToken);
    Task<CursorPageResult<Project>> GetProjects(GetProjectsFilter filter, CancellationToken cancellationToken);
    Task<Project?> GetProjectById(string teamId, string projectId, CancellationToken cancellationToken);
    Task<Project> CreateProject(string teamId, string name, CancellationToken cancellationToken);
}
