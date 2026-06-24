namespace ClaudeMem.Admin.Api.Tests.Infrastructure
{
    internal sealed class Constants
    {
        public static class ProblemTypes
        {
            public const string ProjectTeamMismatch = "/problems/project-team-mismatch";
            public const string JobNotFound = "/problems/job-not-found";
            public const string JobNotInFailedState = "/problems/job-not-in-failed-state";
            public const string ApiKeyNotFound = "/problems/api-key-not-found";
            public const string TeamNotFound = "/problems/team-not-found";
        }
    }
}
