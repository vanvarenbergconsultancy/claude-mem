namespace ClaudeMem.Admin.Ui.Services;

internal static class CacheKeys
{
    internal const string TeamsAll = "teams:all";
    internal const string ProjectsAll = "projects:*";
    internal static string ProjectsForTeam(string teamId) => $"projects:{teamId}";
}
