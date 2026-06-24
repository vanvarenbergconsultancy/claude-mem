using ClaudeMem.Admin.Api.Infrastructure.Results;

namespace ClaudeMem.Admin.Api.Features.Projects.Shared;

/// <summary>Extension methods for project-scoped validation helpers.</summary>
internal static class ProjectExtensions
{
    /// <summary>Returns the appropriate <see cref="ErrorCode"/> if the team or project ownership check failed, otherwise <c>null</c>.</summary>
    public static ErrorCode? ValidateTeamAndProjectIdMisMatch(this ProjectTeamExistence accessResult)
    {
        if (!accessResult.TeamExists)
        {
            return ResultErrorCodes.TeamNotFound;
        }

        if (!accessResult.BelongsToTeam)
        {
            return ResultErrorCodes.ProjectTeamMismatch;
        }

        return null;
    }
}