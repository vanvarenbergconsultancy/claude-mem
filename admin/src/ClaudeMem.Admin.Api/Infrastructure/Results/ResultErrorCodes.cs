namespace ClaudeMem.Admin.Api.Infrastructure.Results;

public static class ResultErrorCodes
{
    public static readonly ErrorCode TeamNotFound           = new("team_not_found",             "Team not found.",                                           ErrorType.NotFound);
    public static readonly ErrorCode ProjectNotFound        = new("project_not_found",          "Project not found.",                                        ErrorType.NotFound);
    // NotFound rather than Conflict or Forbidden: returning 409/403 leaks that the resource exists under another tenant, enabling enumeration attacks.
    public static readonly ErrorCode ProjectTeamMismatch    = new("project_team_mismatch",      "The project does not belong to the specified team.",        ErrorType.NotFound);
    public static readonly ErrorCode ApiKeyNotFound         = new("api_key_not_found",          "Api key not found.",                                        ErrorType.NotFound);
    public static readonly ErrorCode ApiKeyTeamMismatch     = new("api_key_team_mismatch",      "The api key does not belong to the specified team.",        ErrorType.NotFound);
    public static readonly ErrorCode ApiKeyProjectMismatch  = new("api_key_project_mismatch",   "The api key does not belong to the specified project.",     ErrorType.NotFound);
    public static readonly ErrorCode JobNotFound            = new("job_not_found",              "Job not found.",                                            ErrorType.NotFound);
    public static readonly ErrorCode JobNotInFailedState    = new("job_not_in_failed_state",    "Job is not in a failed state.",                             ErrorType.Validation);
}
