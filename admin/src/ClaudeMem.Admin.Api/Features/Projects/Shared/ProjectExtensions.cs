using ClaudeMem.Admin.Api.Infrastructure.Results;

namespace ClaudeMem.Admin.Api.Features.Projects.Shared;

internal static class ProjectExtensions
{
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